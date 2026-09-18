package geaip.bundle

import rego.v1

verification := {
    "bundleId": input.bundleId,
    "version": input.version,
    "sha256Digest": input.sha256Digest,
    "environment": input.environment,
    "signatureValid": true,
    "verificationEvidenceReference": sprintf("evidence://demo/opa/bundles/%s/%s", [input.bundleId, input.sha256Digest]),
    "verifiedAt": input.activatedAt,
} if {
    input.bundleId == "geaip-demo-intent-registration"
    input.version == "2026.09.18"
    input.sha256Digest == "041941cce6753212eb98bd3f0ef73b0772d14df0be6a6dea141c18b6041ac4bd"
    input.environment == "IntegrationDemo"
    input.trustAnchorReference == "trust://demo/opa/demo-opa-signing-01"
}
