using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RBI.Authentication.Internal;

namespace RBI.Authentication;

/// <summary>Registers RBI confidential OIDC authentication and server-side token sessions.</summary>
public static class RbiAuthenticationServiceCollectionExtensions
{
    /// <summary>Registers authentication using the <c>Authentication</c> configuration section.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configure">An optional code-based options override.</param>
    /// <returns>The application service collection.</returns>
    /// <remarks>A shared <c>IDistributedCache</c> must be registered before this method.</remarks>
    public static IServiceCollection AddRbiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RbiAuthenticationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
        {
            throw new InvalidOperationException(
                "RBI.Authentication requires an explicitly registered shared IDistributedCache. " +
                "Register the production cache before AddRbiAuthentication; in-memory cache is for tests and local development only.");
        }

        var options = services.AddOptions<RbiAuthenticationOptions>()
            .Bind(configuration.GetSection(RbiAuthenticationOptions.SectionName));
        if (configure is not null) options.Configure(configure);
        options.ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IValidateOptions<RbiAuthenticationOptions>, RbiAuthenticationOptionsValidator>());
        services.AddDataProtection();
        services.TryAddSingleton(TimeProvider.System);
        services.AddAntiforgery(antiforgery =>
        {
            antiforgery.HeaderName = "X-CSRF-TOKEN";
            antiforgery.Cookie.Name = "__Host-rbi-antiforgery";
            antiforgery.Cookie.HttpOnly = true;
            antiforgery.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            antiforgery.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddAuthentication(authentication =>
        {
            authentication.DefaultScheme = RbiAuthenticationSchemes.Cookie;
            authentication.DefaultAuthenticateScheme = RbiAuthenticationSchemes.Cookie;
            authentication.DefaultSignInScheme = RbiAuthenticationSchemes.Cookie;
            authentication.DefaultChallengeScheme = RbiAuthenticationSchemes.OpenIdConnect;
        })
        .AddCookie(RbiAuthenticationSchemes.Cookie)
        .AddOpenIdConnect(RbiAuthenticationSchemes.OpenIdConnect, _ => { });

        services.AddOptions<CookieAuthenticationOptions>(RbiAuthenticationSchemes.Cookie)
            .Configure(cookie =>
            {
                cookie.Cookie.Name = RbiAuthenticationSchemes.CookieName;
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.Cookie.Path = "/";
                cookie.ExpireTimeSpan = RbiAuthenticationDefaults.SessionLifetime;
                cookie.SlidingExpiration = false;
                cookie.EventsType = typeof(RbiCookieAuthenticationEvents);
            });

        services.AddOptions<OpenIdConnectOptions>(RbiAuthenticationSchemes.OpenIdConnect)
            .Configure<IOptionsMonitor<RbiAuthenticationOptions>, IHostEnvironment>((oidc, authentication, environment) =>
            {
                var configured = authentication.CurrentValue;
                oidc.Authority = configured.Authority;
                oidc.ClientId = configured.ClientId;
                oidc.ClientSecret = configured.ClientSecret;
                oidc.RequireHttpsMetadata = !environment.IsDevelopment() ||
                    configured.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                oidc.ResponseType = OpenIdConnectResponseType.Code;
                oidc.UsePkce = true;
                oidc.SaveTokens = false;
                oidc.GetClaimsFromUserInfoEndpoint = true;
                oidc.MapInboundClaims = false;
                oidc.MaxAge = RbiAuthenticationDefaults.SessionLifetime;
                oidc.Scope.Clear();
                foreach (var scope in RbiAuthenticationDefaults.Scopes)
                    oidc.Scope.Add(scope);
                oidc.TokenValidationParameters.NameClaimType = RbiAuthenticationDefaults.NameClaimType;
                oidc.TokenValidationParameters.RoleClaimType = RbiAuthenticationDefaults.RoleClaimType;
                oidc.EventsType = typeof(RbiOpenIdConnectEvents);
            });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.AddScoped<RbiCookieAuthenticationEvents>();
        services.AddScoped<RbiOpenIdConnectEvents>();
        services.AddSingleton<IRbiTokenSessionStore, RbiDistributedTokenSessionStore>();
        services.AddScoped<IRbiAuthenticationSessionContext, RbiAuthenticationStateSessionContext>();
        services.AddScoped<RbiUserAccessTokenProvider>();
        services.AddScoped<RbiUserAccessTokenHandler>();
        services.AddSingleton<IRbiRefreshTokenClient, RbiOidcRefreshTokenClient>();
        services.AddSingleton<RbiRefreshCoordinator>();
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<RbiRefreshCoordinator>());

        return services;
    }
}
