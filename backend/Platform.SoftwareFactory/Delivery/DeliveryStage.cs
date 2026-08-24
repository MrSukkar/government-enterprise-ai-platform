namespace Platform.SoftwareFactory.Delivery;

public enum DeliveryStage
{
    Intent,
    EnterpriseContext,
    ExistingArchitecture,
    ApprovedPackages,
    AiPlanning,
    CodeGeneration,
    StaticValidation,
    SecurityValidation,
    Sandbox,
    Tests,
    HumanReview,
    Git,
    CiCd,
    Artifact,
    Deployment,
    Registration,
    Observability,
    Evidence
}
