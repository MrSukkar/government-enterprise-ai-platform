# Integration Demo Stage 08 runtime contract

Stage 08 extends the localhost-only synthetic demonstration through governed Code Generation. It does not provide an AI runtime, trust material, generated content, or signed database records in Git.

Before `-StartInfrastructure`, deployment operations must provision outside Git:

- `.runtime/integration-demo-stage-08/packages/seed.sql` with the signed Stage 06 synthetic package catalog.
- `.runtime/integration-demo-stage-08/ai-planning/seed.sql` with the signed Stage 07 planning prompt.
- `.runtime/integration-demo-stage-08/code-generation/seed.sql` with prompt `demo-internal-service-code-generation`, version `1.0.0`, exact UTF-8 content `Generate inert, unapplied source text for the authorized internal service using only the approved packages, planning evidence, and permitted output paths.`, SHA-256 `d01ae4012a2b374a3e9b1ec305472dbfb9bee8b131f2719c3505c365963246a8`, and a valid deployment-trusted signature.
- `Platform:SoftwareFactory:ApprovedPackagesTrust`, `Platform:SoftwareFactory:AiPlanningRuntime`, and `Platform:SoftwareFactory:CodeGenerationRuntime` through deployment-controlled configuration. Generation and evaluation endpoints, profiles, operator identities, and signing-key sets must be distinct as required by each runtime contract.
- Identity-provider, OPA, PostgreSQL, and Neo4j settings through deployment-controlled configuration.

Repository defaults remain empty and fail closed. The policy permits only runtime profile `demo-sovereign-code-generator`, five exact prerequisite evidence references, the two exact Stage 06 package coordinates, four non-execution constraints, and two safe repository-relative output paths. The result is inert candidate data: it cannot invoke tools or commands, write or apply files, install packages, advance the delivery run, invoke Static Validation, or affect production.
