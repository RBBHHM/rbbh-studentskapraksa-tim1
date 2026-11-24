using System.Net.Mail;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class EmailSettings
{
    public string From { get; set; } = "";
    public string SendEmailWithHtmlBodyUrl { get; set; } = "";

    public bool IsEmpty => string.IsNullOrWhiteSpace(From) && string.IsNullOrWhiteSpace(SendEmailWithHtmlBodyUrl);

    public bool IsConfiguredFor(IHostEnvironment environment)
    {
        if (IsEmpty) return environment.IsDevelopment() || environment.IsEnvironment("Testing");

        return MailAddress.TryCreate(From, out var sender) && sender.Address == From &&
            Uri.TryCreate(SendEmailWithHtmlBodyUrl, UriKind.Absolute, out var endpoint) &&
            (endpoint.Scheme == Uri.UriSchemeHttps ||
                endpoint.Scheme == Uri.UriSchemeHttp && endpoint.Host.EndsWith(".rbbh", StringComparison.OrdinalIgnoreCase)) &&
            endpoint.UserInfo.Length == 0 && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0;
    }
}
