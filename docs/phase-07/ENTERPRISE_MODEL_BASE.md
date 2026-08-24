# Phase 07 — Enterprise Model Base

Status: **Implemented**  
Depends on: **Phase 06 — OpenAPI Contract**

## Enterprise Object

The base aggregate implements every field required by the Master Specification:

`Identity + Type + State + Owner + Classification + Relationships + Policies + Actions + Source + Confidence + Evidence + Lifecycle + Timestamps`

Every object is tenant-scoped. Confidence is normalized to `0..1`, timestamps cannot move backward, and the model validates before persistence.

## Relationships

Relationship knowledge states are exactly:

- `Confirmed`
- `Discovered`
- `Inferred`
- `Unknown`

A confirmed relationship requires an evidence reference. Each relationship also records its source, confidence, and observation timestamp.

## Persistence boundary

`IEnterpriseObjectRepository` defines tenant-scoped retrieval and persistence without binding the domain to PostgreSQL or Neo4j. Physical storage, graph projection, migrations, and consistency mechanisms remain implementation concerns of their approved phases.

## Security boundary

Data classification is a shared domain concept in `Platform.Domain.Security`, allowing identity clearance and enterprise resources to use one ordered classification vocabulary without creating a dependency from the Enterprise Model to the Identity module.

