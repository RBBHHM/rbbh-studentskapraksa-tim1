namespace RBBH.RelatedPartiesRegister.Web.Api;

// Matches the backend AdminOnly policy's DB-owned application role. Never use
// the identity provider's ClaimsPrincipal.IsInRole for application authorization.
public static class ApplicationAccess
{
    // This is the effective DB-owned profile, not identity-provider roles.
    // Backend transformation emits no application roles for unknown/inactive users.
    public static bool IsKnownApplicationUser(ApplicationProfile profile) => profile.Roles.Any(role =>
        role is "gost" or "FL" or "PL" or "limiti" or "kapital" or "admin");
    public static bool IsAdministrator(ApplicationProfile profile) => profile.Roles.Contains("admin", StringComparer.Ordinal);
}
