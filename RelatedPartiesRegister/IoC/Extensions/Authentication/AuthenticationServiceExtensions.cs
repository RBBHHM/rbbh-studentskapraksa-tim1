using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication
{
    public static class AuthenticationServiceExtensions
    {
        /// <summary>
        /// Registruje JWT Bearer autentikaciju prema Keycloak-u i RoleClaimsTransformation
        /// koja čita realm_access.roles iz JWT tokena i mapira ih na ClaimTypes.Role.
        /// </summary>
        public static IServiceCollection AddAuthenticationExtension(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var issuer = configuration["KeycloakSettings:Issuer"];

            var publicIssuer = configuration["KeycloakSettings:PublicIssuer"];

            var audience = configuration["KeycloakSettings:Audience"];

            var enabled = configuration.GetValue<bool?>("KeycloakSettings:Enabled")
                ?? !string.IsNullOrWhiteSpace(issuer);

            if (!enabled || string.IsNullOrWhiteSpace(issuer))
            {
                services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
                    .AddScheme<DevelopmentAuthenticationOptions, DevelopmentAuthenticationHandler>(
                        DevelopmentAuthenticationHandler.SchemeName,
                        options => options.Enabled = environment.IsDevelopment());
                AddApplicationAuthorization(services);
                services.AddSingleton(new AuthenticationStartupWarning(
                    environment.IsDevelopment()
                        ? "Keycloak nije konfigurisan; koristi se lokalni razvojni korisnik."
                        : "Keycloak nije konfigurisan; zaštićeni pozivi nisu dostupni."));
                return services;
            }

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    var validIssuers = string.IsNullOrEmpty(publicIssuer)
                        ? new[] { issuer }
                        : new[] { issuer, publicIssuer };

                    options.Authority = issuer;
                    options.RequireHttpsMetadata = !issuer.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase);
                    options.Audience = audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuers = validIssuers,
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.AutomaticRefreshInterval = TimeSpan.FromMinutes(10);
                    options.RefreshInterval = TimeSpan.FromSeconds(10);

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception is SecurityTokenExpiredException)
                                context.Response.Headers.Append("Token-Expired", "true");

                            if (context.Exception is SecurityTokenInvalidSigningKeyException)
                                context.Response.Headers.Append("SigningKey-Invalid", "true");

                            return Task.CompletedTask;
                        }
                    };
                });

            // PL-18: Registracija transformacije rola iz Keycloak JWT-a
            services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
            AddApplicationAuthorization(services);

            return services;
        }

        private static void AddApplicationAuthorization(IServiceCollection services) =>
            services.AddAuthorization(options =>
                options.AddPolicy("application-administration", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    foreach (var role in ApplicationAccessRoles.All)
                        policy.RequireRole(role);
                }));
    }

    public sealed record AuthenticationStartupWarning(string Message);
}
