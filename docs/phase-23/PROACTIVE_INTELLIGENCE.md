# Phase 23 — Proactive Intelligence

## Purpose

Phase 23 correlates authorized operational signals with Enterprise Model context to produce evidence-grounded observations, investigations, and governed-action recommendations. It is advisory and cannot execute an action.

## Governed input

Requests require `enterprise.intelligence.evaluate`, tenant and subject identity, purpose, environment, explicit object scope, maximum classification, a completed observation window, and a signed detection-policy reference with version and SHA-256 digest. Policy thresholds are supplied by approved policy; the platform invents no SLO or alert threshold.

`IProactiveIntelligenceContextProvider` authorizes access before returning objects and trace-linked operational signals. The engine revalidates policy signature, policy identity/version/digest, tenant, scope, classification, time window, unique identities, and authorization evidence. Signals outside the governed window or scope fail closed.

## Findings and recommendations

`IProactiveIntelligenceAnalyzer` remains an abstraction. Its candidates are treated as untrusted and must cite only authorized signals associated with the same enterprise object and only evidence available in the authorized snapshot. Findings receive a deterministic SHA-256 fingerprint for deduplication and stable tracking.

Findings are `Observe`, `Investigate`, or `RecommendGovernedAction`. Only the last may name a proposed action. Every finding is structurally non-effecting, requires human review, and any named action must enter the Phase 20 governance path for identity, approval, OPA, evidence, and MCP enforcement.

## Phase boundary

Phase 23 adds no government packaging, Front Door, automatic remediation, or direct tool execution. Those remain in their approved later phases.
