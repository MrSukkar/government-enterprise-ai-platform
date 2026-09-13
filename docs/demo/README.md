# Government Demonstration Pack

Status: **Prepared for local demonstration planning**

This pack turns the completed **Create Internal Service** vertical slice into a repeatable government demonstration without weakening the platform's fail-closed posture.

## Reference scenario

The reference scenario is **National Permit Renewal — Internal Service**. It was selected because it is understandable to business and technical audiences, has a clear public outcome, requires institutional context and human accountability, and can be demonstrated entirely with synthetic data.

## Use this pack in order

1. [`NATIONAL_PERMIT_RENEWAL_SCENARIO.md`](NATIONAL_PERMIT_RENEWAL_SCENARIO.md) — story, roles, inputs, governed journey, and presenter script.
2. [`ENVIRONMENT_PROMOTION_PLAN.md`](ENVIRONMENT_PROMOTION_PLAN.md) — Local, Integration Demo, Pilot, and Production gates.
3. [`DEMO_ACCEPTANCE_CHECKLIST.md`](DEMO_ACCEPTANCE_CHECKLIST.md) — objective entry, execution, failure-path, and exit checks.
4. [`sample-data/permit-renewal-intent.json`](sample-data/permit-renewal-intent.json) — synthetic intent payload matching the approved OpenAPI contract.

## Safety boundary

- All identities, people, systems, decisions, and evidence references in this pack are synthetic.
- Local preview proves UI, contract, build, and fail-closed behavior; it does not claim institutional runtime readiness.
- No mock adapter may be represented as a production control.
- No external credential, signing material, trust anchor, or organization-specific policy belongs in Git.
- Pilot and Production activation require explicit institutional approval and deployment-controlled configuration.
- No direct `AI -> Production` path is introduced.
