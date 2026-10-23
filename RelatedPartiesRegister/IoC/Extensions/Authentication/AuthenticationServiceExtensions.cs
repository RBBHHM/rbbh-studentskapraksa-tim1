using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RBBH.ConnectedParties.Helpers.Constants;
using System.Security.Claims;

namespace RBBH.ConnectedParties.IoC.Extensions.Authentication
{
    public static class AuthenticationServiceExtensions
    {
        /// <summary>
        /// Registruje Keycloak identitet; aplikacijske role/dozvole dolaze iz baze
        /// kroz RoleClaimsTransformation, nikad iz provider permission/role claimova.
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
            var clientId = configuration["KeycloakSettings:ClientId"];
            var clientSecret = configuration["KeycloakSettings:ClientSecret"];

            var enabled = configuration.GetValue<bool?>("KeycloakSettings:Enabled")
                ?? !string.IsNullOrWhiteSpace(issuer);

            services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

            if (!enabled || string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
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

            // HTTP metadata is a local Development exception, not a string-prefix
            // exception for production or hosts such as localhost.attacker.test.
            var localDevelopmentMetadata = environment.IsDevelopment()
                && Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri)
                && issuerUri.Scheme == Uri.UriSchemeHttp && issuerUri.IsLoopback;

            // Fail closed: a missing audience would otherwise silently accept tokens
            // issued to any client in the realm. Development keeps the documented
            // local behavior (audience check skipped only while it is unset).
            if (string.IsNullOrWhiteSpace(audience) && !environment.IsDevelopment())
                throw new InvalidOperationException(
                    "KeycloakSettings:Audience mora biti postavljen kada je Keycloak uključen izvan Development okruženja.");

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "KeycloakSelector";
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddPolicyScheme("KeycloakSelector", null, options =>
                {
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            ? JwtBearerDefaults.AuthenticationScheme
                            : CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                })
                .AddJwtBearer(options =>
                {
                    var validIssuers = string.IsNullOrEmpty(publicIssuer)
                        ? new[] { issuer }
                        : new[] { issuer, publicIssuer };

                    options.Authority = issuer;
                    options.RequireHttpsMetadata = !localDevelopmentMetadata;
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
                })
                .AddOpenIdConnect("KeycloakOidc", options =>
                {
                    options.Authority = issuer;
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.RequireHttpsMetadata = !localDevelopmentMetadata;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = OpenIdConnectResponseMode.Query;
                    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.MapInboundClaims = false;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.NonceCookie.SameSite = SameSiteMode.Lax;
                    options.Scope.Add("roles");
                    options.TokenValidationParameters.NameClaimType = "name";
                    options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                });

            // Application policies consume only current database-owned grants.
            AddApplicationAuthorization(services);

            return services;
        }

        public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                AddPermissionPolicy(options, ApplicationPolicies.PhysicalPersonsRead, ApplicationPermissions.PhysicalPersonsView);
                AddWritePolicy(options, ApplicationPolicies.PhysicalPersonsWrite, ApplicationPermissions.PhysicalPersonsCreate, ApplicationPermissions.PhysicalPersonsEdit, ApplicationPermissions.PhysicalPersonsDelete);
                AddPermissionPolicy(options, ApplicationPolicies.PhysicalPersonsCreate, ApplicationPermissions.PhysicalPersonsCreate);
                AddPermissionPolicy(options, ApplicationPolicies.PhysicalPersonsEdit, ApplicationPermissions.PhysicalPersonsEdit);
                AddPermissionPolicy(options, ApplicationPolicies.PhysicalPersonsDelete, ApplicationPermissions.PhysicalPersonsDelete);
                AddPermissionPolicy(options, ApplicationPolicies.LegalPersonsRead, ApplicationPermissions.LegalPersonsView);
                AddWritePolicy(options, ApplicationPolicies.LegalPersonsWrite, ApplicationPermissions.LegalPersonsCreate, ApplicationPermissions.LegalPersonsEdit, ApplicationPermissions.LegalPersonsDelete);
                AddPermissionPolicy(options, ApplicationPolicies.LegalPersonsCreate, ApplicationPermissions.LegalPersonsCreate);
                AddPermissionPolicy(options, ApplicationPolicies.LegalPersonsEdit, ApplicationPermissions.LegalPersonsEdit);
                AddPermissionPolicy(options, ApplicationPolicies.LegalPersonsDelete, ApplicationPermissions.LegalPersonsDelete);
                AddPermissionPolicy(options, ApplicationPolicies.CapitalRead, ApplicationPermissions.CapitalView);
                AddWritePolicy(options, ApplicationPolicies.CapitalWrite, ApplicationPermissions.CapitalCreate, ApplicationPermissions.CapitalEdit, ApplicationPermissions.CapitalDelete);
                AddPermissionPolicy(options, ApplicationPolicies.CapitalCreate, ApplicationPermissions.CapitalCreate);
                AddPermissionPolicy(options, ApplicationPolicies.CapitalEdit, ApplicationPermissions.CapitalEdit);
                AddPermissionPolicy(options, ApplicationPolicies.CapitalDelete, ApplicationPermissions.CapitalDelete);
                // Reporting exception approved for this application: reading/downloading
                // is open to every known application user (Guest baseline is granted only
                // to an active AppUser, never to an unknown identity); generating persists
                // a snapshot and requires the database-owned Kapital/Admin create permission.
                options.AddPolicy(ApplicationPolicies.ReportsRead, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(ApplicationAccessRoles.Guest));
                AddPermissionPolicy(options, ApplicationPolicies.ReportsGenerate, ApplicationPermissions.CapitalCreate);
                AddPermissionPolicy(options, ApplicationPolicies.LimitsRead, ApplicationPermissions.LimitsView);
                AddWritePolicy(options, ApplicationPolicies.LimitsWrite, ApplicationPermissions.LimitsCreate, ApplicationPermissions.LimitsEdit, ApplicationPermissions.LimitsDelete);
                AddPermissionPolicy(options, ApplicationPolicies.LimitsCreate, ApplicationPermissions.LimitsCreate);
                AddPermissionPolicy(options, ApplicationPolicies.LimitsEdit, ApplicationPermissions.LimitsEdit);
                AddPermissionPolicy(options, ApplicationPolicies.LimitsDelete, ApplicationPermissions.LimitsDelete);
                AddPermissionPolicy(options, ApplicationPolicies.CodeListsRead, ApplicationPermissions.CodeListsView);
                AddPermissionPolicy(options, ApplicationPolicies.AdministrationRead, ApplicationPermissions.AdministrationView);
                AddPermissionPolicy(options, ApplicationPolicies.AdministrationWrite, ApplicationPermissions.AdministrationManage);
                AddPermissionPolicy(options, ApplicationPolicies.AdminOnly, ApplicationPermissions.AdministrationManage);
            });

            return services;
        }

        private static void AddPermissionPolicy(
            Microsoft.AspNetCore.Authorization.AuthorizationOptions options,
            string name,
            string permission) =>
            options.AddPolicy(name, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("permission", permission));

        private static void AddWritePolicy(
            Microsoft.AspNetCore.Authorization.AuthorizationOptions options,
            string name,
            params string[] permissions) =>
            options.AddPolicy(name, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => permissions.Any(permission =>
                    context.User.HasClaim("permission", permission))));
    }

    public sealed record AuthenticationStartupWarning(string Message);
}
