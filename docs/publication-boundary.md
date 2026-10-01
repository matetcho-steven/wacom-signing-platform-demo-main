# Public Repository Boundary

This repository is intentionally a **portfolio case study**, not a dump of the private Signy product repository.

## Safe to publish here

- architecture diagrams and engineering decisions
- screenshots with non-sensitive/demo data
- clean-room sample code that illustrates patterns
- API contracts using fictional tenants/customers
- automated tests for public samples
- CI workflows that use no private secrets
- explanations of debugging, reliability and security decisions

## Kept private

- production source code and proprietary implementation details
- customer/tenant data and database files
- API keys, JWT signing material, credentials and environment files
- TLS/code-signing certificates and private keys
- private infrastructure addresses and operational configuration
- Wacom commercial licence material or redistributable components subject to third-party terms
- internal business documents and confidential stakeholder communications

## Review rule before publishing

Every public change should answer three questions:

1. Does this reveal a credential, customer detail or production endpoint?
2. Does this copy private product code when a smaller clean-room example would prove the same skill?
3. Does this include third-party material that may have redistribution restrictions?

If any answer is uncertain, keep the material private until reviewed.

The CI workflow also performs a lightweight check for obvious secret-like patterns. It is a guardrail, not a substitute for human review.
