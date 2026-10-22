using RBI.ApiCore;
using RBI.Authentication;
using RBBH.RelatedPartiesRegister.Web.Security;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public static class AppApi
{
    public static Uri? AddAppApi(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment, IdentityConfiguration identity)
    {
        var value = configuration["Apis:BaseUrl"];
        if (string.IsNullOrWhiteSpace(value))
        {
            services.AddScoped<IProfileApi, UnconfiguredProfileApi>();
            return null;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            (!environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            value != value.Trim() || !uri.AbsolutePath.EndsWith('/'))
            throw new InvalidOperationException("Apis:BaseUrl must be an absolute HTTP(S) URL ending in '/', without credentials, query or fragment; HTTPS is required outside Development.");

        var client = services.AddRbiApiClient<IProfileApi, ProfileApi>("Profile", options =>
        {
            options.BaseUrl = uri;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        if (identity.Enabled) client.AddRbiUserAccessToken();
        return uri;
    }
}
