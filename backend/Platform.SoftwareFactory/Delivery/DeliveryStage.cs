namespace Platform.SoftwareFactory.Delivery;

public enum DeliveryStage
{
    Intent,
    EnterpriseContext,
    ExistingSystems,
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
    OpenTelemetry,
    AutomaticRegistration,
    EnterpriseModel,
    Evidence
}
