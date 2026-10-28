using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore.Internal;
using RBI.Authentication.Internal;

namespace RBI.Authentication;

/// <summary>Adds signed-in user token propagation to RBI API clients.</summary>
public static class RbiAuthenticatedApiClientBuilderExtensions
{
    /// <summary>Adds the current user's access token to a named RBI API client.</summary>
    /// <param name="builder">The API client's HTTP builder.</param>
    /// <returns>The same builder for further configuration.</returns>
    public static IHttpClientBuilder AddRbiUserAccessToken(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRbiApiLogicalRequestHandler<RbiUserAccessTokenHandler>();
        builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
        });
        return builder;
    }
}
