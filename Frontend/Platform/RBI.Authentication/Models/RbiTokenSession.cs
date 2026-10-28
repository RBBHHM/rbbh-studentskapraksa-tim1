namespace RBI.Authentication.Internal;

internal sealed record RbiTokenSession(
    string AccessToken,
    string RefreshToken,
    string? IdToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset SessionExpiresAt);
