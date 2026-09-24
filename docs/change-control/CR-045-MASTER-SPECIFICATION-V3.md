# CR-045 — Adopt Project Master Specification V3

Status: **Proposed — owner approval required**

Predecessor: `docs/change-control/CR-044-INTEGRATION-PLATFORM-CAPABILITY.md`.

## Requested decision

Approve `docs/PROJECT_MASTER_SPECIFICATION_V3.md` as the sole implementation authority and approve `docs/V3_CAPABILITY_ROADMAP.md` as its sequential delivery map.

## Change

The proposed V3:

- preserves the completed V2 foundation, constitutional invariants, accepted addenda, and Phase 01–30 history;
- adds the six Integration Platform Capability layers approved for specification development in CR-044;
- defines Security, Compliance, Sovereignty, and HA/DR as disqualifying hard gates;
- defines product-neutral contracts and a governed product-decision method;
- creates twelve sequential **V3 Capability Increments**, not Phase 31;
- preserves unconfigured, fail-closed repository defaults and prohibits direct AI-to-Production authority.

## Impact analysis

| Area | Impact |
|---|---|
| Architecture | Extends the existing platform authority; no implementation boundary changes are made by this CR. |
| Historical delivery | Phases 01–30 and all accepted V2 increments, waves, and demo stages remain complete and immutable. |
| Governance | V3 becomes authoritative only after explicit approval, acceptance, fast-forward merge, and green main CI. |
| Security/compliance | Existing controls are preserved and integration-specific controls become mandatory. |
| Sovereignty | Air-gapped operation and local control planes remain mandatory design capabilities. |
| HA/DR | Targets must derive from workload and business-impact evidence; no numbers are invented. |
| Products | No vendor or product is selected. |
| Runtime | No runtime, deployment, credential, trust material, or successful evidence is created by this CR. |

## Alternatives

1. Keep V2 as permanent authority and reject the capability.
2. Create a separate post-Phase30 program, previously scored lower in CR-044.
3. Approve V3 as proposed.

## Recommendation

Approve V3 as proposed. It maintains one identity, policy, sovereignty, observability, Enterprise Model, and Evidence authority while keeping each new increment independently gated.

## Approval effect

Approval authorizes V3 Capability Increment V3-01 to begin under its own Change Control and acceptance gate. It does not approve any named product, institutional pilot, Production deployment, public endpoint, credential, private key, certificate, trust material, or direct AI action.

Owner decision: **Pending**.
