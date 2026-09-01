using System.Net;
using System.Net.Mail;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using Microsoft.Extensions.Options;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// SMTP implementacija — šalje prave emailove.
/// Aktivira se postavkom Email:Provider = "smtp" u appsettings.json.
/// Popuniti Email:Smtp sekciju s podacima bankinog mail servera.
/// </summary>
public sealed class SmtpEmailService(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private string AppBaseUrl => options.Value.AppBaseUrl;

    public async Task SendUnlockRequestAsync(
        string adminEmail, string requestedBy, int year, int month, string reason)
    {
        var subject = $"Zahtjev za otključavanje perioda — {EmailTemplates.MonthDisplay(year, month)}";
        await SendAsync(adminEmail, subject,
            EmailTemplates.UnlockRequest(requestedBy, year, month, reason, AppBaseUrl));
    }

    public async Task SendNeedsInfoNotificationAsync(
        string userEmail, string requestedBy, int year, int month, string adminNote, Guid requestId)
    {
        var subject = $"Zahtjev za otključavanje — potrebne dodatne informacije ({EmailTemplates.MonthDisplay(year, month)})";
        await SendAsync(userEmail, subject,
            EmailTemplates.NeedsInfo(requestedBy, year, month, adminNote, AppBaseUrl));
    }

    public async Task SendUserResponseAsync(
        string adminEmail, string requestedBy, int year, int month, string userMessage)
    {
        var subject = $"Odgovor korisnika na zahtjev — {EmailTemplates.MonthDisplay(year, month)}";
        await SendAsync(adminEmail, subject,
            EmailTemplates.UserResponse(requestedBy, year, month, userMessage, AppBaseUrl));
    }

    public async Task SendUnlockConfirmationAsync(
        string userEmail, string requestedBy, int year, int month)
    {
        var subject = $"Period otključan — {EmailTemplates.MonthDisplay(year, month)}";
        await SendAsync(userEmail, subject,
            EmailTemplates.UnlockConfirmation(requestedBy, year, month, AppBaseUrl));
    }

    public async Task SendHrNewPhysicalPersonAsync(
        string hrEmail, string personName, string createdBy,
        string relationBasis, DateTime? dateFrom, DateTime? dateTo)
    {
        var subject = $"Novo fizičko lice u registru — {personName}";
        await SendAsync(hrEmail, subject,
            EmailTemplates.HrNewPhysicalPerson(personName, createdBy, relationBasis, dateFrom, dateTo, AppBaseUrl));
    }

    public async Task SendHrPhysicalPersonExpiredAsync(
        string hrEmail, string personName, string updatedBy,
        string relationBasis, DateTime dateTo)
    {
        var subject = $"Istekao osnov povezanosti — {personName}";
        await SendAsync(hrEmail, subject,
            EmailTemplates.HrPhysicalPersonExpired(personName, updatedBy, relationBasis, dateTo, AppBaseUrl));
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtp = options.Value.Smtp;

#pragma warning disable SYSLIB0006
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl   = smtp.EnableSsl,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password)
        };
#pragma warning restore SYSLIB0006

        using var msg = new MailMessage(
            new MailAddress(smtp.FromAddress, smtp.FromName),
            new MailAddress(to))
        {
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };

        await client.SendMailAsync(msg);
        logger.LogInformation("Email poslan na {To}: {Subject}", to, subject);
    }
}
