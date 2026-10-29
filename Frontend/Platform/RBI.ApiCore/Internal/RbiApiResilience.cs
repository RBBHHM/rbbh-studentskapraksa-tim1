using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace RBI.ApiCore.Internal;

internal static class RbiApiResilience
{
    internal const int MaximumRetryCount = 2;

    internal static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> pipeline)
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = MaximumRetryCount,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldRetryAfterHeader = true,
            ShouldHandle = arguments => ValueTask.FromResult(
                UsesStandardProfile(arguments.Context) &&
                IsSafeMethod(arguments.Context.GetRequestMessage()?.Method) &&
                IsTransient(arguments.Outcome)),
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 8,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = arguments => ValueTask.FromResult(
                UsesStandardProfile(arguments.Context) && IsTransient(arguments.Outcome)),
        });
    }

    private static bool UsesStandardProfile(ResilienceContext context) =>
        RbiApiRequestContextHandler.UsesStandardResilience(context.GetRequestMessage());

    private static bool IsSafeMethod(HttpMethod? method) =>
        method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;

    private static bool IsTransient(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is HttpRequestException) return true;
        return outcome.Result is { } response && IsTransientStatus(response.StatusCode);
    }

    private static bool IsTransientStatus(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.RequestTimeout or
        HttpStatusCode.TooManyRequests or
        HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout;
}
