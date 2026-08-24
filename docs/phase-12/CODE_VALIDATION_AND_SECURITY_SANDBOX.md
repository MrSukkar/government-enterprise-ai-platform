# Phase 12 — Code Validation & Security Sandbox

Status: **Implemented**  
Depends on: **Phase 11 — AI Development Engine**

## Validation gates

`CodeValidationPipeline` separates static validation from security validation. Static controls run only after Code Generation; security controls run only after Static Validation. A gate fails closed when no controls are registered, a control is incomplete, evidence is absent, or an Error/Critical finding exists.

Controls are vendor-neutral asynchronous interfaces. Compiler checks, analyzers, dependency validation, secret scanning, SAST, license checks, and other approved controls can be attached without changing the workflow authority.

## Firecracker-class sandbox policy

General .NET execution requires a policy that validates all of the following:

- `Firecracker-class` isolation;
- ephemeral microVM execution;
- no production credentials;
- no host-filesystem access;
- network default deny with explicit destinations only;
- configured CPU, memory, and execution-time limits;
- an exact institutionally approved sandbox image.

The policy requires limits but does not invent their numerical values; environments set them after workload and risk validation. WASM remains specialized and is not treated as the general .NET security sandbox.

## Execution boundary

Sandbox execution is reachable only after Security Validation. Requests carry references rather than production secrets. Results record exit status, timeout, isolation violations, produced-artifact references, and mandatory evidence. Only a zero-exit, non-timeout, non-violation result is accepted.

`ISecuritySandboxRuntime` is vendor neutral so sovereign deployments can provide a local Firecracker-class implementation with no external control-plane dependency.

