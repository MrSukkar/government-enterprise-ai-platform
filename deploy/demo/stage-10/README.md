# Integration Demo Stage 10 runtime contract

Stage 10 extends the localhost-only synthetic demonstration through governed deterministic Security Validation. It does not provide control trust material, control signatures, candidate content, or signed database records in Git.

Deployment operations must provision the complete Stage 09 runtime inputs outside Git plus `Platform:SoftwareFactory:SecurityValidationRuntime`. Exactly two signed Security control profiles are required: `demo-security-data-flow` and `demo-security-input-boundary`. They must use deterministic rules, carry approval evidence, verify under pinned institutional trust, and remain distinct from every Static control identity.

Repository defaults remain empty and fail closed. The policy permits only the exact accepted Static report and inert candidate binding, the two named Security controls, the Developer role, Internal maximum classification, and `security-report` output. Controls inspect inert candidate data in process only. They cannot mutate source, compile, run commands or processes, acquire packages, call external analyzers, configure networks, execute the candidate or Sandbox, advance workflow, or affect production.
