# Phase 26 — Developer Experience

## Purpose

Phase 26 defines a governed, repeatable developer golden path on top of the Software Factory. It reduces setup ambiguity without weakening approved packages, architecture, validation, sandbox, human review, Git, CI/CD, provenance, or evidence boundaries.

## Approved workspace request

Requests require `developer.workspace.bootstrap`, tenant and developer identity, purpose, a safe relative workspace path, Git repository and branch, local package source, time, and an approved template. Absolute paths and parent traversal are rejected.

Templates carry identity, version, SHA-256 digest, signature, architecture reference, required .NET SDK version, explicit approved packages, and evidence. `IDeveloperTemplateVerifier` validates the template before environment inspection and returned identity/digest/signature evidence is revalidated fail-closed.

## Sovereign environment readiness

`IDeveloperEnvironmentInspector` reports SDK, Git, local package-source availability, production credentials, outbound-network requirement, evidence, and time. A valid environment must match the approved SDK, have Git and the local source, require no outbound network, contain no production credentials, and provide current evidence.

## Deterministic golden path

The generated plan has contiguous typed stages:

1. materialize the signed template;
2. restore locked dependencies from the local source;
3. run the governed project verifier;
4. build without restore;
5. test without rebuild;
6. review changes;
7. submit through Git and CI.

Tools and argument arrays are explicit rather than an arbitrary shell script. Review and Git/CI submission require human confirmation. Plans are structurally unable to deploy to production and are atomically registered with evidence; returned registry state is revalidated.

## Phase boundary

Phase 26 adds no direct AI-to-production path and does not yet close operational feedback into delivery; the Closed Loop Engine begins in Phase 27.
