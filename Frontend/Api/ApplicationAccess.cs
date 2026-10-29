namespace RBBH.RelatedPartiesRegister.Web.Api;

// Matches the backend AdminOnly policy's DB-owned application role. Never use
// the identity provider's ClaimsPrincipal.IsInRole for application authorization.
public static class ApplicationAccess
{
    // This is the effective DB-owned profile, not identity-provider roles.
    // Backend emits virtual Guest for unmapped identities; disabled identities receive no grants.
    public static bool HasEffectiveApplicationAccess(ApplicationProfile profile) => profile.Roles.Any(role =>
        role is "gost" or "FL" or "PL" or "limiti" or "kapital" or "admin");
    public static bool IsAdministrator(ApplicationProfile profile) => profile.Roles.Contains("admin", StringComparer.Ordinal);
}
