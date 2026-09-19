# Integration Demo Stage 07 runtime contract

Stage 07 extends the localhost-only synthetic demonstration through governed AI Planning. It does not provide an AI runtime, trust material, or signed database records in Git.

Before `-StartInfrastructure`, deployment operations must provision outside Git:

- `.runtime/integration-demo-stage-07/packages/seed.sql` with the signed Stage 06 synthetic package catalog.
- `.runtime/integration-demo-stage-07/ai-planning/seed.sql` with prompt `demo-internal-service-planning`, version `1.0.0`, exact UTF-8 content `Produce a non-executable implementation plan for the authorized internal service using only the approved packages and context.`, SHA-256 `996dc1d8964bab91d4db217c9e0d3d439898c34d76f95de1ad546d4061298fef`, and a valid deployment-trusted signature.
- `Platform:SoftwareFactory:ApprovedPackagesTrust` and `Platform:SoftwareFactory:AiPlanningRuntime` through deployment-controlled configuration. Generation and evaluation endpoints, profiles, operator identities, and signing keys must be distinct where the runtime contract requires separation.
- Identity-provider, OPA, PostgreSQL, and Neo4j settings through deployment-controlled configuration.

The repository defaults remain empty and fail closed. The policy permits only runtime profile `demo-sovereign-planner`, four exact prerequisite evidence references, the two exact Stage 06 package coordinates, and four non-execution constraints. The resulting candidate cannot invoke tools, create files, advance the delivery run, invoke Code Generation, or affect production.
