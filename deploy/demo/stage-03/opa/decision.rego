package geaip

import rego.v1

decision := registration_response("Permit", ["demo_intent_registration_permitted"]) if {
    registration_permitted
}

decision := registration_response("Deny", ["demo_intent_registration_denied"]) if {
    input.action == "internal-service.intent.register"
    not registration_permitted
}

decision := context_response("Permit", ["demo_enterprise_context_permitted"], context_scope) if {
    context_permitted
}

decision := context_response("Deny", ["demo_enterprise_context_denied"], null) if {
    input.action == "internal-service.enterprise-context.discover"
    not context_permitted
}

registration_permitted if {
    common_permit
    input.action == "internal-service.intent.register"
    input.classification in {"Public", "Internal"}
}

context_permitted if {
    common_permit
    input.action == "internal-service.enterprise-context.discover"
    input.classification == "Internal"
    input.attributes.registrationVersion == "0"
    input.attributes.intentSha256Digest == "6d4b8c396f0d0b1f692b9aa7f4c0a9bc5ee26d5775f6e74bdab76a01ec3d7d1e"
}

common_permit if {
    input.environment == "IntegrationDemo"
    input.tenantId == "demo-permit-authority"
    input.purpose == "internal-service-delivery-demonstration"
    input.verifiedPolicyBundle.signatureValid == true
    input.verifiedPolicyBundle.bundleId == "geaip-demo-stage-03"
    input.verifiedPolicyBundle.version == "2026.09.18"
    input.verifiedPolicyBundle.sha256Digest == "847bc7c3f57336bfa5cd44d09d9463b23eb1fcee62df71ab8721d791c2a91d0c"
}

context_scope := {
    "maximumClassification": "Internal",
    "allowedResourceIds": [
        "enterprise-object:permit-renewal-policy",
        "enterprise-object:permit-renewal-service"
    ],
    "allowedModalities": ["Graph"],
    "requiredRoles": [],
    "maximumResults": 2,
    "existingSystems": null,
    "existingArchitecture": null,
    "approvedPackages": null,
    "aiPlanning": null,
    "codeGeneration": null,
    "staticValidation": null,
    "securityValidation": null,
    "sandbox": null,
    "tests": null,
    "humanReview": null,
    "git": null,
    "ciCd": null,
    "artifact": null,
    "deployment": null,
    "openTelemetry": null,
    "automaticRegistration": null,
    "enterpriseModel": null,
    "evidenceCompletion": null,
}

registration_response(outcome, reasons) := response(outcome, reasons, null)
context_response(outcome, reasons, scope) := response(outcome, reasons, scope)

response(outcome, reasons, scope) := {
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
    "scope": scope,
    "decidedAt": input.evaluatedAt,
}
