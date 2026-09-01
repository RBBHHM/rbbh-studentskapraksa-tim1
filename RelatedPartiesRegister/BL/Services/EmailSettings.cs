namespace RBBH.ConnectedParties.BL.Services;

public sealed class EmailSettings
{
    public string Provider    { get; set; } = "demo"; // "demo" | "smtp"
    public string AdminEmail  { get; set; } = "admin@raiffeisenbank.ba";
    public string HrEmail     { get; set; } = "hr@raiffeisenbank.ba";
    public string AppBaseUrl  { get; set; } = "http://localhost:5001";
    public SmtpSettings Smtp  { get; set; } = new();
}

public sealed class SmtpSettings
{
    public string Host        { get; set; } = "";
    public int    Port        { get; set; } = 587;
    public bool   EnableSsl   { get; set; } = true;
    public string Username    { get; set; } = "";
    public string Password    { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@raiffeisenbank.ba";
    public string FromName    { get; set; } = "Raiffeisen Bank BiH";
}
