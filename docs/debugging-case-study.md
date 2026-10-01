# Debugging Case Study: Cross-Process Settings Contention

This is a sanitized account of a real failure found during physical Windows pilot testing. Names, paths and product-specific implementation details have been removed; no private source code is reproduced.

## Symptom

A signed document could be saved successfully, but the completion workflow occasionally failed immediately afterwards when another process attempted to read machine-local protected settings.

The failure was intermittent and appeared as a file-sharing / access error rather than a signing error, which initially made the signing workflow look responsible.

## Investigation

The application was composed of multiple Windows processes that shared one protected machine-local settings file.

The settings component already used an in-process `lock`, so concurrent threads inside one process were serialized correctly. Physical testing showed the assumption was incomplete: a `lock` only coordinates threads **inside the same process**. It cannot prevent a desktop process, background service and launcher process from touching the same file at the same time.

That distinction explained why normal single-process testing passed while the installed multi-process workflow could still fail.

## Root cause

The synchronization boundary was too small.

```text
Incorrect mental model
----------------------
process A threads ----> in-process lock ----> settings file

Actual system
-------------
Desktop process  -----\
Service process  ------> shared protected settings file
Launcher process -----/
```

Each process had its own independent in-memory lock. Nothing coordinated file access across process boundaries.

## Fix strategy

The correction used several defensive layers rather than a single retry:

1. Introduce a cross-process exclusive lock visible to every participating process.
2. Keep the existing in-process synchronization for inexpensive thread safety.
3. Retry only transient file-sharing failures for a bounded period instead of retrying every exception indefinitely.
4. Preserve atomic replacement semantics for durable writes so readers never observe a partially-written settings file.
5. Use unique temporary files and clean them up after failures.
6. Leave the signing/ERP business workflow unchanged; fix the storage concurrency boundary instead of masking the symptom higher in the stack.

## Validation approach

The fix was validated against the installed multi-process workflow, not only unit-level logic:

- reproduce the original completion sequence
- keep the background components running concurrently
- complete a signing operation
- confirm settings reads no longer fail during the transition
- confirm the completed document still follows the normal completion/audit path
- verify the change did not alter unrelated ERP/signing semantics

## Engineering lesson

The important lesson was not "add retries." It was to identify the **real concurrency boundary**.

A synchronization mechanism is only correct if its scope matches all actors that share the resource. When an application evolves from one process into several cooperating processes, assumptions that were safe in-process may become invalid even though the code still looks thread-safe locally.
