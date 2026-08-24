namespace Platform.Web.Foundation;

public sealed class ExperienceContext
{
    public const string UnauthenticatedPersona = "No governed persona selected";
    public string Persona { get; private set; } = UnauthenticatedPersona;
    public string Purpose { get; private set; } = "Platform orientation";
    public bool IsGovernedIdentityEstablished => false;

    public void SetPreviewContext(string persona, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persona);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        Persona = persona;
        Purpose = purpose;
    }
}
