using System.Net.Http.Headers;
using RBI.ApiCore;
using RBI.ApiCore.Internal;

namespace RBI.Authentication.Internal;

internal sealed class RbiUserAccessTokenHandler(
    RbiUserAccessTokenProvider tokenProvider) : IRbiApiLogicalRequestHandler
{
    public async ValueTask<RbiApiLogicalRequestResult> SendAsync(
        HttpRequestMessage request,
        RbiApiSendDelegate next,
        CancellationToken cancellationToken)
    {
        var result = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (result.Status == RbiTokenAcquisitionStatus.Success)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return RbiApiLogicalRequestResult.Success(await next(request, cancellationToken));
        }

        return RbiApiLogicalRequestResult.Failure(
            result.Status == RbiTokenAcquisitionStatus.ReauthenticationRequired
                ? new RbiApiError(
                    RbiApiErrorKind.Unauthorized,
                    null,
                    "Authentication is required.",
                    "The user session must be renewed before this request can be sent.")
                : new RbiApiError(
                    RbiApiErrorKind.Unavailable,
                    null,
                    "Authentication is temporarily unavailable.",
                    "The identity provider could not refresh the user session."));
    }
}
