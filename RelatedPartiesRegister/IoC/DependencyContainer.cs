using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.DL.Mapper;
using RBBH.ConnectedParties.IoC.Extensions.Controllers;
using RBBH.ConnectedParties.IoC.Extensions.CORS;
using RBBH.ConnectedParties.IoC.Extensions.Databases;
using RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling;
using RBBH.ConnectedParties.IoC.Extensions.Health;
using RBBH.ConnectedParties.IoC.Extensions.HTTP;
using RBBH.ConnectedParties.IoC.Extensions.Logging;
using RBBH.ConnectedParties.IoC.Extensions.Swagger;

namespace RBBH.ConnectedParties.IoC
{
    public static class DependencyContainer
    {
        public static IServiceCollection AddServicesExtensions(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env, IHostBuilder host)
        {
            #region Register builder services for Dependency Injection

            services
                .AddCORSExtension(configuration, env)
                .AddControllersExtension()
                .AddExceptionHandlingExtension(env)
                .AddHealthCheckExtension(configuration)
                .AddSwaggerExtension()
                .AddAuthenticationExtension(configuration, env)
                .AddDatabaseExtension(configuration, env)
                .AddHTTPExtension(configuration);

            host.AddSerilogExtension();

            #endregion

            MapsterConfiguration.RegisterMappings();

            #region Register application service classes with their interfaces

            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ICodeListService, CodeListService>();
            services.AddScoped<IRelatedPersonService, RelatedPersonService>();
            services.AddHttpClient<KeycloakAdminService>();
            services.AddScoped<IAppUserService, AppUserService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IPeriodLockRepository, PeriodLockRepository>();
            services.AddScoped<IUnlockRequestRepository, UnlockRequestRepository>();
            services.AddScoped<IPeriodLockService, PeriodLockService>();
            services.AddScoped<ILegalEntityService, LegalEntityService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ILimitService, LimitService>();
            services.AddScoped<ICapitalService, CapitalService>();
            // ── Email ─────────────────────────────────────────────────────────
            services.AddOptions<EmailSettings>().Bind(configuration.GetSection("Email"))
                .Validate(settings => settings.Provider.Equals("demo", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(settings.AdminEmail) && !string.IsNullOrWhiteSpace(settings.HrEmail) &&
                     (settings.Provider.Equals("httpapi", StringComparison.OrdinalIgnoreCase)
                         ? !string.IsNullOrWhiteSpace(settings.From) && Uri.TryCreate(settings.SendEmailWithHtmlBodyUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps
                         : settings.Provider.Equals("smtp", StringComparison.OrdinalIgnoreCase) &&
                           !string.IsNullOrWhiteSpace(settings.Smtp.Host) && !string.IsNullOrWhiteSpace(settings.Smtp.FromAddress))),
                    "Email transport, sender and required recipients must be explicitly configured.")
                .ValidateOnStart();
            services.AddSingleton<EmailLogStore>();

            var emailProvider = configuration["Email:Provider"] ?? "demo";
            if (emailProvider.Equals("httpapi", StringComparison.OrdinalIgnoreCase))
                services.AddScoped<IEmailService, HttpApiEmailService>();
            else if (emailProvider.Equals("smtp", StringComparison.OrdinalIgnoreCase))
                services.AddScoped<IEmailService, SmtpEmailService>();
            else
                services.AddScoped<IEmailService, MockEmailService>();

            #endregion

            return services;
        }
    }
}
