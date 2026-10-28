namespace RBI.Authentication.Internal;

internal static class RbiAuthenticationSchemes
{
    internal const string Cookie = "RBI.Cookie";
    internal const string OpenIdConnect = "RBI.OpenIdConnect";
    internal const string SessionClaim = "rbi:session_id";
    internal const string CookieName = "__Host-rbi-session";
}
