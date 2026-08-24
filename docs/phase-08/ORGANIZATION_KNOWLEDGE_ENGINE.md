# Phase 08 — Organization Knowledge Engine

Status: **Implemented**  
Depends on: **Phase 07 — Enterprise Model Base**

## Controlled GraphRAG path

The implementation follows the approved sequence:

`Identity -> Purpose -> Policy -> Authorized Scope -> Graph/Vector/Lexical -> Fusion/Reranking -> Authorization Re-check -> AI Context`

`KnowledgeQuery` cannot be accepted without an authenticated identity context, declared purpose, tenant, non-empty resource allow-list, classification ceiling, retrieval modality, required roles, and result limit. The initial access decision occurs before any source is called.

## Retrieval sources

`IKnowledgeRetrievalSource` is a vendor-neutral source contract. Implementations receive `AuthorizedRetrievalScope`, never an unrestricted query. Modalities are:

- Graph — Neo4j is the approved baseline implementation in a later integration phase.
- Vector — an abstraction only; pgvector and Qdrant remain conditional.
- Lexical — an abstraction for authorized lexical retrieval.

Source output outside tenant, resource, classification, modality, or score boundaries causes a hard failure. It is not silently accepted and filtered later.

## Fusion and context release

The deterministic fusion service deduplicates and orders candidates without an AI dependency. After fusion, every candidate is re-authorized against its actual tenant, resource, and classification. Only approved candidates enter `AuthorizedKnowledgeContext` for later AI use.

## Deferred

- Physical Neo4j indexes, connectors, and graph projections.
- Conditional vector-store selection and benchmarking.
- AI generation, evaluation, and model runtime.
- Knowledge APIs and user experiences.

