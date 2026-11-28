using System.Security.Claims;

namespace RBBH.ConnectedParties.Helpers;

internal static class RegistryVerification
{
    internal const string SelfDenied = "verification.self_denied";

    internal static bool IsOwnRecord(string? createdBy, string username, IHttpContextAccessor? accessor)
    {
        if (string.IsNullOrWhiteSpace(createdBy)) return false;
        var principal = accessor?.HttpContext?.User;
        var email = principal?.FindFirst(ClaimTypes.Email)?.Value ?? principal?.FindFirst("email")?.Value;
        return string.Equals(createdBy.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(createdBy.Trim(), email?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
