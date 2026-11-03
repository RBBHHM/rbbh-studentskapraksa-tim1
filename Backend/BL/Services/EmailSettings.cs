namespace RBBH.ConnectedParties.BL.Services;

public sealed class EmailSettings
{
    public string Provider { get; set; } = "demo"; // "demo" | "smtp" | "httpapi"
    public string AdminEmail { get; set; } = "";
    public string HrEmail { get; set; } = "";
    public string AppBaseUrl { get; set; } = "http://localhost:5001";
    public string From { get; set; } = "";
    public string? SendEmailWithHtmlBodyUrl { get; set; }
    public SmtpSettings Smtp { get; set; } = new();

    public bool IsConfiguredFor(IHostEnvironment environment)
    {
        var local = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (string.Equals(Provider, "demo", StringComparison.OrdinalIgnoreCase)) return local;
        if (!Address(AdminEmail) || !Uri.TryCreate(AppBaseUrl, UriKind.Absolute, out var app) ||
            !(app.Scheme == Uri.UriSchemeHttps || local && app.Scheme == Uri.UriSchemeHttp && app.IsLoopback) ||
            app.UserInfo.Length != 0 || app.Query.Length != 0 || app.Fragment.Length != 0) return false;
        // HR messages are retired. Do not require an unrelated mailbox to run the active outbox.
        if (string.Equals(Provider, "httpapi", StringComparison.OrdinalIgnoreCase))
            return Address(From) && Uri.TryCreate(SendEmailWithHtmlBodyUrl, UriKind.Absolute, out var api) &&
                api.Scheme == Uri.UriSchemeHttps && api.UserInfo.Length == 0 && api.Fragment.Length == 0;
        return string.Equals(Provider, "smtp", StringComparison.OrdinalIgnoreCase) && Smtp is not null &&
            !string.IsNullOrWhiteSpace(Smtp.Host) && Smtp.Port is > 0 and <= 65535 && Address(Smtp.FromAddress) && (local || Smtp.EnableSsl);
    }

    private static bool Address(string? value) => !string.IsNullOrWhiteSpace(value) &&
        System.Net.Mail.MailAddress.TryCreate(value, out var address) && address.Address == value;
}

public sealed class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Raiffeisen Bank BiH";
}
