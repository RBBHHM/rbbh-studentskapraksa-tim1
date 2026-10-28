namespace RBI.Authentication.Internal;

internal static class RbiAuthenticationDefaults
{
    internal static readonly string[] Scopes =
        ["openid", "profile", "email", "roles", "offline_access"];

    internal const string NameClaimType = "name";
    internal const string RoleClaimType = "roles";
    internal static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
}
