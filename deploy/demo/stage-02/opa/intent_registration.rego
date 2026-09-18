package geaip.intent

import rego.v1

registration := response("Permit", ["demo_intent_registration_permitted"]) if {
    input.action == "internal-service.intent.register"
    input.environment == "IntegrationDemo"
    input.tenantId == "demo-permit-authority"
    input.purpose == "internal-service-delivery-demonstration"
    input.classification in {"Public", "Internal"}
    input.verifiedPolicyBundle.signatureValid == true
    input.verifiedPolicyBundle.bundleId == "geaip-demo-intent-registration"
    input.verifiedPolicyBundle.version == "2026.09.18"
    input.verifiedPolicyBundle.sha256Digest == "041941cce6753212eb98bd3f0ef73b0772d14df0be6a6dea141c18b6041ac4bd"
}

registration := response("Deny", ["demo_intent_registration_denied"]) if {
    not permitted
}

permitted if {
    input.action == "internal-service.intent.register"
    input.environment == "IntegrationDemo"
    input.tenantId == "demo-permit-authority"
    input.purpose == "internal-service-delivery-demonstration"
    input.classification in {"Public", "Internal"}
    input.verifiedPolicyBundle.signatureValid == true
    input.verifiedPolicyBundle.bundleId == "geaip-demo-intent-registration"
    input.verifiedPolicyBundle.version == "2026.09.18"
    input.verifiedPolicyBundle.sha256Digest == "041941cce6753212eb98bd3f0ef73b0772d14df0be6a6dea141c18b6041ac4bd"
}

response(outcome, reasons) := {
    "decisionRequestId": input.decisionRequestId,
    "action": input.action,
    "resourceId": input.resourceId,
    "bundleId": input.verifiedPolicyBundle.bundleId,
    "bundleVersion": input.verifiedPolicyBundle.version,
    "bundleSha256Digest": input.verifiedPolicyBundle.sha256Digest,
    "environment": input.environment,
    "outcome": outcome,
    "reasons": reasons,
    "evidenceReferences": [sprintf("evidence://demo/opa/decisions/%s", [input.decisionRequestId])],
    "scope": null,
    "decidedAt": input.evaluatedAt,
}
