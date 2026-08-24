namespace Platform.Identity.Access;

public sealed record AccessDecision(bool IsAllowed, string Code, string Reason)
{
    public static AccessDecision Allow() => new(true, "access_allowed", "The governed access requirements are satisfied.");
    public static AccessDecision Deny(string code, string reason) => new(false, code, reason);
}
