using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Email;
using Microsoft.Extensions.Options;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Šalje emailove preko zajedničkog bankovnog email mikroservisa
/// (isti endpoint koji koristi rbbh-code-data-products — "Email:SendEmailWithHtmlBodyUrl").
/// Aktivira se postavkom Email:Provider = "httpapi" u appsettings.json.
/// </summary>
public sealed class HttpApiEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailSettings> options,
    EmailLogStore store,
    ILogger<HttpApiEmailService> logger) : IEmailService
{
    private string AppBaseUrl => options.Value.AppBaseUrl;
    private string FromAddress => options.Value.From;

    public Task SendUnlockRequestAsync(
        string adminEmail, string requestedBy, int year, int month, string reason)
    {
        var subject = $"Zahtjev za otključavanje perioda — {EmailTemplates.MonthDisplay(year, month)}";
        return SendAsync(adminEmail, subject,
            EmailTemplates.UnlockRequest(requestedBy, year, month, reason, AppBaseUrl), "admin");
    }

    public Task SendNeedsInfoNotificationAsync(
        string userEmail, string requestedBy, int year, int month, string adminNote, Guid requestId)
    {
        var subject = $"Zahtjev za otključavanje — potrebne dodatne informacije ({EmailTemplates.MonthDisplay(year, month)})";
        return SendAsync(userEmail, subject,
            EmailTemplates.NeedsInfo(requestedBy, year, month, adminNote, AppBaseUrl), "user", requestId);
    }

    public Task SendUserResponseAsync(
        string adminEmail, string requestedBy, int year, int month, string userMessage)
    {
        var subject = $"Odgovor korisnika na zahtjev — {EmailTemplates.MonthDisplay(year, month)}";
        return SendAsync(adminEmail, subject,
            EmailTemplates.UserResponse(requestedBy, year, month, userMessage, AppBaseUrl), "admin");
    }

    public Task SendUnlockConfirmationAsync(
        string userEmail, string requestedBy, int year, int month)
    {
        var subject = $"Period otključan — {EmailTemplates.MonthDisplay(year, month)}";
        return SendAsync(userEmail, subject,
            EmailTemplates.UnlockConfirmation(requestedBy, year, month, AppBaseUrl), "user");
    }

    public Task SendHrNewPhysicalPersonAsync(
        string hrEmail, string personName, string createdBy,
        string relationBasis, DateTime? dateFrom, DateTime? dateTo)
    {
        var subject = $"Novo fizičko lice u registru — {personName}";
        return SendAsync(hrEmail, subject,
            EmailTemplates.HrNewPhysicalPerson(personName, createdBy, relationBasis, dateFrom, dateTo, AppBaseUrl), "hr");
    }

    public Task SendHrPhysicalPersonExpiredAsync(
        string hrEmail, string personName, string updatedBy,
        string relationBasis, DateTime dateTo)
    {
        var subject = $"Istekao osnov povezanosti — {personName}";
        return SendAsync(hrEmail, subject,
            EmailTemplates.HrPhysicalPersonExpired(personName, updatedBy, relationBasis, dateTo, AppBaseUrl), "hr");
    }

    public async Task SendPeriodLockedBroadcastAsync(
        IEnumerable<string> recipientEmails, string lockedBy, int year, int month)
    {
        var subject = $"Period zaključan — {EmailTemplates.MonthDisplay(year, month)}";
        var html = EmailTemplates.PeriodLocked(lockedBy, year, month, AppBaseUrl);
        foreach (var to in recipientEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            await SendAsync(to, subject, html, "broadcast");
    }

    public async Task SendPeriodUnlockedBroadcastAsync(
        IEnumerable<string> recipientEmails, string unlockedBy, int year, int month)
    {
        var subject = $"Period otključan — {EmailTemplates.MonthDisplay(year, month)}";
        var html = EmailTemplates.PeriodUnlocked(unlockedBy, year, month, AppBaseUrl);
        foreach (var to in recipientEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            await SendAsync(to, subject, html, "broadcast");
    }

    private async Task SendAsync(string to, string subject, string htmlBody, string audience, Guid? requestId = null)
    {
        var emailApiUrl = options.Value.SendEmailWithHtmlBodyUrl;
        if (string.IsNullOrWhiteSpace(emailApiUrl) || string.IsNullOrWhiteSpace(FromAddress) || string.IsNullOrWhiteSpace(to))
        {
            logger.LogWarning(
                "Email transport, pošiljalac ili primalac nije konfigurisan; poruka nije poslana.");
            throw new InvalidOperationException("Email konfiguracija nije potpuna.");
        }

        var emailDto = new EmailDto
        {
            From = FromAddress,
            To = to,
            Subject = subject,
            Text = htmlBody
        };

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.PostAsJsonAsync(emailApiUrl, emailDto);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Slanje emaila na {To} nije uspjelo. Status: {StatusCode}", to, response.StatusCode);
                return;
            }

            store.Add(new EmailLogEntry
            {
                To = to,
                Subject = subject,
                HtmlBody = htmlBody,
                Audience = audience,
                RequestId = requestId
            });
            logger.LogInformation("Email poslan preko zajedničkog servisa na {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Greška prilikom slanja emaila na {To} preko zajedničkog servisa.", to);
        }
    }
}
