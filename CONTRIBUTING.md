# Contribution Workflow

This portfolio repository uses a small production-style workflow so changes remain reviewable.

1. Open or reference an issue describing the change and acceptance criteria.
2. Create a focused branch from `main`.
3. Keep commits scoped and readable.
4. Open a pull request that explains the problem, the implementation and validation.
5. Require CI to pass before merging.
6. Review every public change against `docs/publication-boundary.md` so private product material is not exposed.

For code samples, prefer a minimal clean-room example that demonstrates the engineering idea rather than copying private production source.
