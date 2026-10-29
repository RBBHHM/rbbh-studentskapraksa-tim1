using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace RBBH.ConnectedParties.IoC.Extensions.Security;

/// <summary>Deployment-owned proxy trust, with no trust-all production fallback.</summary>
public static class ForwardedHeaderConfiguration
{
    public static IServiceCollection AddApplicationForwardedHeaders(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var proxies = configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        var networks = configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [];
        var limit = configuration.GetValue<int?>("ReverseProxy:ForwardLimit");
        var hosts = (configuration["AllowedHosts"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!environment.IsDevelopment()
            && (proxies.Length + networks.Length == 0 || limit is null || hosts.Length == 0 || hosts.Any(host => host.Contains('*'))))
            throw new InvalidOperationException(
                "Configure ReverseProxy:KnownProxies or KnownNetworks, ForwardLimit and explicit AllowedHosts outside Development.");
        if (limit is <= 0)
            throw new InvalidOperationException("ReverseProxy:ForwardLimit must be positive.");

        var trustedProxies = proxies.Select(value => IPAddress.TryParse(value, out var address)
            ? address : throw new InvalidOperationException("ReverseProxy:KnownProxies must contain IP addresses.")).ToArray();
        var trustedNetworks = networks.Select(value => System.Net.IPNetwork.TryParse(value, out var network) && network.PrefixLength > 0
            ? network : throw new InvalidOperationException("ReverseProxy:KnownNetworks must contain bounded CIDR networks.")).ToArray();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = limit ?? 1;
            // Keep framework loopback defaults for unconfigured local Development.
            if (trustedProxies.Length + trustedNetworks.Length > 0)
            {
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var proxy in trustedProxies) options.KnownProxies.Add(proxy);
                foreach (var network in trustedNetworks) options.KnownIPNetworks.Add(network);
            }
            foreach (var host in hosts) options.AllowedHosts.Add(host);
        });
        return services;
    }
}
