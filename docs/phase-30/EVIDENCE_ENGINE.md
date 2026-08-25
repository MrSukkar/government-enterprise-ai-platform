# Phase 30 — Evidence Engine and Cryptographic Proof

## Approved evidence chain

The engine enforces the exact sequence required by PROJECT MASTER SPECIFICATION v2:

`Request -> Context -> Knowledge -> Decision -> Policy -> Approval -> Action -> Result -> Telemetry -> Evidence`

Each tenant-scoped chain uses monotonically increasing sequence numbers. Every entry contains the previous entry's SHA-256 digest, a canonical SHA-256 digest of its own governed fields, a signature envelope, authorization evidence, trace references, correlation identity, classification, purpose, actor, and occurrence time.

## Append-only and tamper-evident behavior

- The storage contract exposes load and atomic append operations only; it exposes no update or deletion operation.
- The next stage is derived from the stored head and cannot be selected out of order.
- Atomic append requires the expected prior sequence and prior digest, preventing lost updates and competing forks.
- Before appending, an existing head is rehashed and its signature is verified.
- The store's returned entry is compared to the signed entry field-by-field.
- Canonical hashing uses length-prefixed UTF-8 fields, invariant formatting, SHA-256, ordered trace references, and the prior digest.

## Cryptographic verification

Verification walks the complete ordered chain and checks tenant and chain identity, sequence, approved stage order, correlation, time order, previous-hash linkage, recomputed entry hash, and signature validity. It returns per-entry proof results, root and head digests, completeness, failures, authorization evidence, and verification time.

Signing and verification are abstractions so sovereign deployments can bind them to the approved PKI, HSM, key-management, certificate-chain, and rotation implementation without a mandatory external service or unapproved algorithm decision.

## Access control and sovereignty

- Append requires `evidence.append` and authorization before reading the chain head.
- Verification requires `evidence.verify` and authorization before reading chain entries.
- The storage read is explicitly scoped by tenant, maximum authorized classification, and authorization evidence.
- Each returned entry's classification is authorized again before it is processed.
- The engine has no external API, SaaS, telemetry, or control-plane dependency and is suitable for air-gapped implementations.
