# Integration Demo Stage 09 runtime contract

Stage 09 extends the localhost-only synthetic demonstration through governed deterministic Static Validation. It does not provide control trust material, control signatures, candidate content, or signed database records in Git.

Before `-StartInfrastructure`, deployment operations must provision outside Git:

- The Stage 08 package, AI Planning, and Code Generation signed seeds under `.runtime/integration-demo-stage-09`.
- Complete prior-stage runtime settings plus `Platform:SoftwareFactory:StaticValidationRuntime` through deployment-controlled configuration.
- Exactly two signed Static control profiles: `demo-static-contract-shape` and `demo-static-source-boundary`. Profiles must use deterministic case-sensitive rules, allow only `.cs` and `.razor` candidate paths as appropriate, carry approval evidence, and verify under pinned institutional trust.
- Identity-provider, OPA, PostgreSQL, and Neo4j settings through deployment-controlled configuration.

Repository defaults remain empty and fail closed. The policy permits only the exact Stage 08 candidate binding, the two named controls, the Developer role, Internal maximum classification, and `static-report` output. The controls inspect inert candidate data in process only. They cannot write source, compile, run commands or processes, acquire packages, call external analyzers, access networks, execute the candidate, advance the delivery run, invoke Security Validation, or affect production.
