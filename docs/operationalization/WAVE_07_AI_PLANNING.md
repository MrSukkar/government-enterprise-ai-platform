# Operationalization Wave 07 — Governed AI Planning Runtime

Wave 07 operationalizes only `Approved Packages -> AI Planning` under approved CR-008.

The runtime revalidates the exact tenant/purpose-bound Approved Packages snapshot and a deterministic delivery-run snapshot stopped at `ApprovedPackages`. Signed-bundle verification and exact action-specific OPA scope occur before prompt or context access. The exact active planning prompt is digest and signature verified. Context is loaded only from the immutable preceding evidence tables and re-authorized per item.

Generation uses a fixed deployment-configured HTTPS endpoint through a provider-neutral planning-only JSON envelope. The envelope forbids tools and generated files, is size/time bounded by exact deployment and OPA values, and accepts only signed responses bound to the exact input. Independent evaluation uses a distinct endpoint, runtime profile, operator identity, signing key, and all six required criteria. The accepted candidate is re-authorized and recorded atomically in PostgreSQL.

Repository defaults contain no endpoint, model, credential, prompt, context, certificate, trust key, or institutional data and remain fail closed. The migration scripts are deployment artifacts only and are not applied automatically.

`IAuthorizedAiPlanningCandidateReader`, Code Generation, workflow advancement, tools, generated files, package operations, institutional mutation, external provisioning, and production action remain disconnected.

Change-control record: `docs/change-control/CR-008-OPERATIONALIZATION-WAVE-07.md`.
