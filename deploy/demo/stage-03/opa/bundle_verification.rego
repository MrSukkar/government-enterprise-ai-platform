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
    input.bundleId == "geaip-demo-stage-03"
    input.version == "2026.09.18"
    input.sha256Digest == "847bc7c3f57336bfa5cd44d09d9463b23eb1fcee62df71ab8721d791c2a91d0c"
    input.environment == "IntegrationDemo"
    input.trustAnchorReference == "trust://demo/opa/demo-opa-signing-01"
}
