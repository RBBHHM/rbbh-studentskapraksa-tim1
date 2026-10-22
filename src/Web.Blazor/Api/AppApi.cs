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
        return uri;
    }
}
