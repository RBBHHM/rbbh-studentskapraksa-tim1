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
