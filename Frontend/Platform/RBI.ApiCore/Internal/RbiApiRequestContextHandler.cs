using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace RBI.ApiCore.Internal;

internal sealed class RbiApiRequestContextHandler(
    IOptionsMonitor<RbiApiClientOptions> options,
    string clientName) : DelegatingHandler
{
    private const string CorrelationHeader = "X-Correlation-ID";
    internal static readonly HttpRequestOptionsKey<bool> StandardResilienceKey =
        new("RBI.ApiCore.StandardResilience");

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var configured = options.Get(clientName);
        request.Options.Set(
            StandardResilienceKey,
            configured.ResilienceProfile == RbiApiResilienceProfile.Standard);

        if (!request.Headers.Contains(CorrelationHeader) && Activity.Current is { } activity)
            request.Headers.TryAddWithoutValidation(CorrelationHeader, activity.TraceId.ToString());

        return base.SendAsync(request, cancellationToken);
    }

    internal static bool UsesStandardResilience(HttpRequestMessage? request) =>
        request is not null &&
        request.Options.TryGetValue(StandardResilienceKey, out var enabled) &&
        enabled;
}
