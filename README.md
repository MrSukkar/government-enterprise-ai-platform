# Government Enterprise AI Platform

Government Enterprise AI Platform is the approved sovereign platform foundation for governed enterprise AI across the institutional lifecycle:

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`

## Foundation status

- The approved 30-phase roadmap is complete through **Phase 30 â€” Evidence Engine**.
- Phase 30 acceptance is **Satisfied**.
- The solution contains 15 .NET 10 projects and uses an ASP.NET Core modular-monolith backend with a Blazor WebAssembly frontend.
- No Phase 31 is approved. Any new phase or architectural deviation requires the approved Change Control process.
- The first business implementation, **Create Internal Service Workspace**, is authorized under CR-001 through Operational Increment 03. Governed intent registration is protected, OPA-gated, idempotent, optimistic-concurrency controlled, and bound to atomic evidence-bearing persistence. Deployment-controlled OPA and repository adapters remain unavailable, so institutional mutation and material execution fail closed.

The implementation authority is [`docs/PROJECT_MASTER_SPECIFICATION_V2.md`](docs/PROJECT_MASTER_SPECIFICATION_V2.md). Current delivery state is recorded in [`PROJECT_STATUS.md`](PROJECT_STATUS.md), and the fixed roadmap is recorded in [`docs/30_PHASE_ROADMAP.md`](docs/30_PHASE_ROADMAP.md).

## Architecture baseline

- PostgreSQL is the primary database baseline.
- Neo4j is the Enterprise Graph baseline; pgvector and Qdrant remain conditional.
- OPA is the Policy Authority. The AI runtime and LLM are not policy or workflow authorities.
- There is no direct `AI -> Production` path.
- Identity, authorization, human approval, Git, CI/CD, security validation, observability, and cryptographic evidence remain mandatory controls.

The approved architecture is a Modular Monolith and must not be converted to microservices without Change Control.

## Build and verification

Open `GovernmentEnterpriseAIPlatform.sln` in **Visual Studio Community 2026**. The solution has been verified with all 15 projects loaded. Command-line verification is also available from PowerShell:

```powershell
dotnet restore .\GovernmentEnterpriseAIPlatform.sln --locked-mode
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-project.ps1
```

The verification script checks the active acceptance gate, the 15-project boundary, approved platform invariants, and the complete solution build.

## Change control

All work beyond the approved foundation follows:

`Change Request -> Impact Analysis -> Architectural Review -> Decision -> Master Specification Update -> Approval`
