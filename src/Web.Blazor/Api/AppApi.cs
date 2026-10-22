using RBI.ApiCore;
using RBI.Authentication;
using RBBH.RelatedPartiesRegister.Web.Security;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public static class AppApi
{
    public static Uri? AddAppApi(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment, IdentityConfiguration identity)
    {
        services.AddTransient<ApplicationCultureHandler>();
        var value = configuration["Apis:BaseUrl"];
        if (string.IsNullOrWhiteSpace(value))
        {
            services.AddScoped<IProfileApi, UnconfiguredProfileApi>();
            services.AddScoped<IRelatedPersonsApi, UnconfiguredRelatedPersonsApi>();
            services.AddScoped<ILegalEntitiesApi, UnconfiguredLegalEntitiesApi>();
            services.AddScoped<ILimitsApi, UnconfiguredLimitsApi>();
            services.AddScoped<ICapitalApi, UnconfiguredCapitalApi>();
            services.AddScoped<IReportsApi, UnconfiguredReportsApi>();
            services.AddScoped<INotificationsApi, UnconfiguredNotificationsApi>();
            services.AddScoped<IAuditApi, UnconfiguredAuditApi>();
            return null;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            (!environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            value != value.Trim() || !uri.AbsolutePath.EndsWith('/'))
            throw new InvalidOperationException("Apis:BaseUrl must be an absolute HTTP(S) URL ending in '/', without credentials, query or fragment; HTTPS is required outside Development.");

        if (!identity.Enabled)
        {
            // Never proxy anonymously to the backend's independent Development
            // authentication fallback. Configured auth uses the RBI token handler.
            services.AddScoped<IProfileApi, UnconfiguredProfileApi>();
            services.AddScoped<IRelatedPersonsApi, UnconfiguredRelatedPersonsApi>();
            services.AddScoped<ILegalEntitiesApi, UnconfiguredLegalEntitiesApi>();
            services.AddScoped<ILimitsApi, UnconfiguredLimitsApi>();
            services.AddScoped<ICapitalApi, UnconfiguredCapitalApi>();
            services.AddScoped<IReportsApi, UnconfiguredReportsApi>();
            services.AddScoped<INotificationsApi, UnconfiguredNotificationsApi>();
            services.AddScoped<IAuditApi, UnconfiguredAuditApi>();
            return uri;
        }

        var client = services.AddRbiApiClient<IProfileApi, ProfileApi>("Profile", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        client.AddHttpMessageHandler<ApplicationCultureHandler>();
        if (identity.Enabled) client.AddRbiUserAccessToken();
        var persons = services.AddRbiApiClient<IRelatedPersonsApi, RelatedPersonsApi>("RelatedPersons", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        persons.AddHttpMessageHandler<ApplicationCultureHandler>();
        if (identity.Enabled) persons.AddRbiUserAccessToken();
        var legal = services.AddRbiApiClient<ILegalEntitiesApi, LegalEntitiesApi>("LegalEntities", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        legal.AddHttpMessageHandler<ApplicationCultureHandler>();
        legal.AddRbiUserAccessToken();
        var limits = services.AddRbiApiClient<ILimitsApi, LimitsApi>("Limits", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        limits.AddHttpMessageHandler<ApplicationCultureHandler>();
        limits.AddRbiUserAccessToken();
        var capital = services.AddRbiApiClient<ICapitalApi, CapitalApi>("Capital", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        capital.AddHttpMessageHandler<ApplicationCultureHandler>();
        capital.AddRbiUserAccessToken();
        var reports = services.AddRbiApiClient<IReportsApi, ReportsApi>("Reports", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(30);
        });
        reports.AddHttpMessageHandler<ApplicationCultureHandler>();
        reports.AddRbiUserAccessToken();
        var notifications = services.AddRbiApiClient<INotificationsApi, NotificationsApi>("Notifications", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        notifications.AddHttpMessageHandler<ApplicationCultureHandler>();
        notifications.AddRbiUserAccessToken();
        var audit = services.AddRbiApiClient<IAuditApi, AuditApi>("Audit", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        audit.AddHttpMessageHandler<ApplicationCultureHandler>();
        audit.AddRbiUserAccessToken();
        return uri;
    }
}
