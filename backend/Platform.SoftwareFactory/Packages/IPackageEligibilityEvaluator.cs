namespace Platform.SoftwareFactory.Packages;

public interface IPackageEligibilityEvaluator
{
    PackageUseDecision Evaluate(InstitutionalPackage package, PackageUseRequest request);
}
