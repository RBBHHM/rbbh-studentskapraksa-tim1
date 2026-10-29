using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace RBI.Authentication.Internal;

internal sealed class RbiAuthenticationOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<RbiAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, RbiAuthenticationOptions options)
    {
        var failures = new List<string>();
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority) ||
            (authority.Scheme != Uri.UriSchemeHttps && authority.Scheme != Uri.UriSchemeHttp))
            failures.Add("Authentication:Authority must be an absolute HTTP or HTTPS URI.");
        else if (!environment.IsDevelopment() && authority.Scheme != Uri.UriSchemeHttps)
            failures.Add("Authentication:Authority must use HTTPS outside Development.");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add("Authentication:ClientId is required.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            failures.Add("Authentication:ClientSecret is required for the confidential OIDC client.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
