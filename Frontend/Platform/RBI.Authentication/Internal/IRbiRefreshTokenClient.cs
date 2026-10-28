namespace RBI.Authentication.Internal;

internal interface IRbiRefreshTokenClient
{
    ValueTask<RbiRefreshResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}

internal enum RbiRefreshStatus
{
    Success,
    ReauthenticationRequired,
    TemporarilyUnavailable,
}

internal sealed record RbiRefreshResult(
    RbiRefreshStatus Status,
    string? AccessToken = null,
    string? RefreshToken = null,
    TimeSpan? ExpiresIn = null)
{
    internal static RbiRefreshResult Success(
        string accessToken,
        string? refreshToken,
        TimeSpan expiresIn) =>
        new(RbiRefreshStatus.Success, accessToken, refreshToken, expiresIn);

    internal static readonly RbiRefreshResult ReauthenticationRequired =
        new(RbiRefreshStatus.ReauthenticationRequired);

    internal static readonly RbiRefreshResult TemporarilyUnavailable =
        new(RbiRefreshStatus.TemporarilyUnavailable);
}
