CREATE CONSTRAINT enterprise_object_identity IF NOT EXISTS
FOR (resource:EnterpriseObject) REQUIRE (resource.tenantId, resource.resourceId) IS UNIQUE;

MERGE (resource:EnterpriseObject {
  tenantId: 'demo-permit-authority',
  resourceId: 'enterprise-object:permit-renewal-policy'
})
SET resource.classification = 'Internal',
    resource.classificationRank = 1,
    resource.contextContent = 'Synthetic demo policy: permit renewal requires an active permit, verified identity, and an eligibility decision recorded with evidence.',
    resource.contextRelevance = 0.99,
    resource.source = 'enterprise-model',
    resource.evidenceReferences = ['evidence://demo/enterprise-context/permit-renewal-policy/001'];

MERGE (resource:EnterpriseObject {
  tenantId: 'demo-permit-authority',
  resourceId: 'enterprise-object:permit-renewal-service'
})
SET resource.classification = 'Internal',
    resource.classificationRank = 1,
    resource.contextContent = 'Synthetic demo service context: National Permit Renewal serves residents and permit officers through a governed renewal journey.',
    resource.contextRelevance = 0.97,
    resource.source = 'enterprise-model',
    resource.evidenceReferences = ['evidence://demo/enterprise-context/permit-renewal-service/001'];

MERGE (resource:EnterpriseObject {
  tenantId: 'demo-permit-authority',
  resourceId: 'enterprise-object:out-of-scope-control'
})
SET resource.classification = 'Internal',
    resource.classificationRank = 1,
    resource.contextContent = 'Synthetic negative fixture that must never be returned by the Stage 03 authorized scope.',
    resource.contextRelevance = 1.0,
    resource.source = 'enterprise-model',
    resource.evidenceReferences = ['evidence://demo/enterprise-context/out-of-scope/001'];
