namespace RBI.Authentication.Internal;

internal enum RbiTokenAcquisitionStatus
{
    Success,
    ReauthenticationRequired,
    TemporarilyUnavailable,
}

internal sealed record RbiTokenAcquisitionResult(
    RbiTokenAcquisitionStatus Status,
    string? AccessToken = null)
{
    internal static RbiTokenAcquisitionResult Success(string accessToken) =>
        new(RbiTokenAcquisitionStatus.Success, accessToken);

    internal static readonly RbiTokenAcquisitionResult ReauthenticationRequired =
        new(RbiTokenAcquisitionStatus.ReauthenticationRequired);

    internal static readonly RbiTokenAcquisitionResult TemporarilyUnavailable =
        new(RbiTokenAcquisitionStatus.TemporarilyUnavailable);
}
