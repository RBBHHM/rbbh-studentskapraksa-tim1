using Microsoft.Extensions.DependencyInjection;

namespace RBI.ApiCore.Internal;

internal delegate ValueTask<HttpResponseMessage> RbiApiSendDelegate(
    HttpRequestMessage request,
    CancellationToken cancellationToken);

internal interface IRbiApiLogicalRequestHandler
{
    ValueTask<RbiApiLogicalRequestResult> SendAsync(
        HttpRequestMessage request,
        RbiApiSendDelegate next,
        CancellationToken cancellationToken);
}

internal sealed record RbiApiLogicalRequestResult(
    HttpResponseMessage? Response,
    RbiApiError? Error)
{
    internal static RbiApiLogicalRequestResult Success(HttpResponseMessage response) =>
        new(response, null);

    internal static RbiApiLogicalRequestResult Failure(RbiApiError error) =>
        new(null, error);
}

internal sealed record RbiApiLogicalRequestHandlerRegistration(string HttpClientName, Type HandlerType);

internal static class RbiApiLogicalRequestHandlerRegistrationExtensions
{
    internal static IHttpClientBuilder AddRbiApiLogicalRequestHandler<THandler>(
        this IHttpClientBuilder builder)
        where THandler : class, IRbiApiLogicalRequestHandler
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Services.Any(descriptor =>
                descriptor.ImplementationInstance is RbiApiLogicalRequestHandlerRegistration registration &&
                string.Equals(registration.HttpClientName, builder.Name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"RBI API client '{builder.Name}' already has a logical request handler.");
        }

        builder.Services.AddSingleton(
            new RbiApiLogicalRequestHandlerRegistration(builder.Name, typeof(THandler)));
        return builder;
    }
}
