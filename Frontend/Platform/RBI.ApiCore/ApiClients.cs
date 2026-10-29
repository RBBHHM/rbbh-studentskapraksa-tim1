using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RBI.ApiCore.Internal;

namespace RBI.ApiCore;

/// <summary>Registers typed HTTP clients backed by the RBI API transport.</summary>
public static class RbiApiServiceCollectionExtensions
{
    /// <summary>Registers a named typed API client and binds its options from <c>Rbi:Apis:{name}</c>.</summary>
    /// <typeparam name="TClient">The consumer-facing client contract.</typeparam>
    /// <typeparam name="TImplementation">The client implementation constructed by dependency injection.</typeparam>
    /// <param name="services">The application service collection.</param>
    /// <param name="name">The stable client name used for configuration and diagnostics.</param>
    /// <param name="configure">An optional code-based options override.</param>
    /// <returns>The underlying HTTP client builder for additional handlers.</returns>
    public static IHttpClientBuilder AddRbiApiClient<TClient, TImplementation>(
        this IServiceCollection services,
        string name,
        Action<RbiApiClientOptions>? configure = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (services.Any(descriptor =>
                descriptor.ImplementationInstance is RbiApiClientRegistration registration &&
                string.Equals(registration.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"RBI API client '{name}' is already registered.");
        }

        services.AddSingleton(new RbiApiClientRegistration(name, typeof(TClient), typeof(TImplementation)));

        var options = services.AddOptions<RbiApiClientOptions>(name)
            .BindConfiguration($"Rbi:Apis:{name}");
        if (configure is not null) options.Configure(configure);
        options
            .Validate(value => IsValidBaseUrl(value.BaseUrl),
                $"RBI API client '{name}' requires a valid absolute HTTP or HTTPS setting at 'Rbi:Apis:{name}:BaseUrl'.")
            .Validate(value => value.Timeout >= TimeSpan.FromMilliseconds(10) && value.Timeout <= TimeSpan.FromMinutes(10),
                $"RBI API client '{name}' Timeout must be between 10 milliseconds and 10 minutes.")
            .Validate(value => Enum.IsDefined(value.ResilienceProfile),
                $"RBI API client '{name}' ResilienceProfile must be None or Standard.")
            .ValidateOnStart();

        var httpClientName = $"RBI.ApiCore:{name}";
        var builder = services.AddHttpClient(httpClientName, (provider, client) =>
        {
            var configured = provider.GetRequiredService<IOptionsMonitor<RbiApiClientOptions>>().Get(name);
            client.BaseAddress = configured.BaseUrl;
            client.Timeout = configured.Timeout;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).RemoveAllLoggers();

        builder.AddHttpMessageHandler(provider => new RbiApiRequestContextHandler(
            provider.GetRequiredService<IOptionsMonitor<RbiApiClientOptions>>(),
            name));
        builder.AddResilienceHandler("standard", RbiApiResilience.Configure);

        services.AddTransient<TClient>(provider =>
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("RBI.ApiCore.Transport");
            var handlerRegistration = provider
                .GetServices<RbiApiLogicalRequestHandlerRegistration>()
                .SingleOrDefault(registration =>
                    string.Equals(registration.HttpClientName, httpClientName, StringComparison.Ordinal));
            var logicalHandler = handlerRegistration is null
                ? null
                : (IRbiApiLogicalRequestHandler)provider.GetRequiredService(handlerRegistration.HandlerType);
            var transport = new RbiApiTransport(client, logger, name, logicalHandler);
            return ActivatorUtilities.CreateInstance<TImplementation>(provider, transport);
        });

        return builder;
    }

    private static bool IsValidBaseUrl(Uri? value) =>
        value is { IsAbsoluteUri: true } &&
        (value.Scheme == Uri.UriSchemeHttp || value.Scheme == Uri.UriSchemeHttps);
}
