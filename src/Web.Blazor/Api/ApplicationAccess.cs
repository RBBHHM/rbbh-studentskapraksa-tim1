namespace RBBH.RelatedPartiesRegister.Web.Api;

// Matches the backend AdminOnly policy's DB-owned application role. Never use
// the identity provider's ClaimsPrincipal.IsInRole for application authorization.
public static class ApplicationAccess
{
    public static bool IsAdministrator(ApplicationProfile profile) => profile.Roles.Contains("admin", StringComparer.Ordinal);
}
