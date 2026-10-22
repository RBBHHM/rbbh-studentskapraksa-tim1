using RBI.ApiCore;
using RBI.Authentication;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public static class AppApi
{
    // One app-owned backend address, also returned for optional codebook wiring.
    // No package option surface or global HttpClient policy is changed.
    public static Uri? AddAppApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (configuration.GetSection("Rbi:Apis").GetChildren().Any(settings => !string.IsNullOrWhiteSpace(settings["BaseUrl"])))
            throw new InvalidOperationException("This starter uses Apis:BaseUrl as its single backend address, not Rbi:Apis:*:BaseUrl.");

        var value = configuration["Apis:BaseUrl"];
        if (string.IsNullOrWhiteSpace(value))
        {
            if (environment.IsDevelopment()) services.AddScoped<IAuditLogApi, EmptyAuditLogApi>();
            else services.AddScoped<IAuditLogApi, UnavailableAuditLogApi>();
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            (!environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            value != value.Trim() || !uri.AbsolutePath.EndsWith('/'))
        {
            throw new InvalidOperationException("Apis:BaseUrl must be an absolute HTTP(S) URL ending in '/', without credentials, query or fragment; HTTPS is required outside Development.");
        }

        var client = services.AddRbiApiClient<IAuditLogApi, AuditLogApi>("Application", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(10);
        });
        client.AddRbiUserAccessToken();
        return uri;
    }
}
