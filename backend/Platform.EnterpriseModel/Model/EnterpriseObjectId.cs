namespace Platform.EnterpriseModel.Model;

public readonly record struct EnterpriseObjectId(Guid Value)
{
    public static EnterpriseObjectId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
