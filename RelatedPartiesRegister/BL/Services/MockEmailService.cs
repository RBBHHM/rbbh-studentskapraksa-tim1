using RBBH.ConnectedParties.BL.ServiceInterfaces;
using Microsoft.Extensions.Options;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class MockEmailService(
    IOptions<EmailSettings> options,
    EmailLogStore store,
    ILogger<MockEmailService> logger) : IEmailService
{
    private string AppBaseUrl => options.Value.AppBaseUrl;

    public Task SendUnlockRequestAsync(
        string adminEmail, string requestedBy, int year, int month, string reason)
    {
        var subject = $"Zahtjev za otključavanje perioda — {EmailTemplates.MonthDisplay(year, month)}";
        var html    = EmailTemplates.UnlockRequest(requestedBy, year, month, reason, AppBaseUrl);
        store.Add(new EmailLogEntry { To = adminEmail, Subject = subject, HtmlBody = html, Audience = "admin" });
        Log(adminEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendNeedsInfoNotificationAsync(
        string userEmail, string requestedBy, int year, int month, string adminNote, Guid requestId)
    {
        var subject = $"Zahtjev za otključavanje — potrebne dodatne informacije ({EmailTemplates.MonthDisplay(year, month)})";
        var html    = EmailTemplates.NeedsInfo(requestedBy, year, month, adminNote, AppBaseUrl);
        store.Add(new EmailLogEntry
        {
            To = userEmail, Subject = subject, HtmlBody = html,
            Audience = "user", RequestId = requestId
        });
        Log(userEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendUserResponseAsync(
        string adminEmail, string requestedBy, int year, int month, string userMessage)
    {
        var subject = $"Odgovor korisnika na zahtjev — {EmailTemplates.MonthDisplay(year, month)}";
        var html    = EmailTemplates.UserResponse(requestedBy, year, month, userMessage, AppBaseUrl);
        store.Add(new EmailLogEntry { To = adminEmail, Subject = subject, HtmlBody = html, Audience = "admin" });
        Log(adminEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendUnlockConfirmationAsync(
        string userEmail, string requestedBy, int year, int month)
    {
        var subject = $"Period otključan — {EmailTemplates.MonthDisplay(year, month)}";
        var html    = EmailTemplates.UnlockConfirmation(requestedBy, year, month, AppBaseUrl);
        store.Add(new EmailLogEntry { To = userEmail, Subject = subject, HtmlBody = html, Audience = "user" });
        Log(userEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendHrNewPhysicalPersonAsync(
        string hrEmail, string personName, string createdBy,
        string relationBasis, DateTime? dateFrom, DateTime? dateTo)
    {
        var subject = $"Novo fizičko lice u registru — {personName}";
        var html    = EmailTemplates.HrNewPhysicalPerson(personName, createdBy, relationBasis, dateFrom, dateTo, AppBaseUrl);
        store.Add(new EmailLogEntry { To = hrEmail, Subject = subject, HtmlBody = html, Audience = "hr" });
        Log(hrEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendHrPhysicalPersonExpiredAsync(
        string hrEmail, string personName, string updatedBy,
        string relationBasis, DateTime dateTo)
    {
        var subject = $"Istekao osnov povezanosti — {personName}";
        var html    = EmailTemplates.HrPhysicalPersonExpired(personName, updatedBy, relationBasis, dateTo, AppBaseUrl);
        store.Add(new EmailLogEntry { To = hrEmail, Subject = subject, HtmlBody = html, Audience = "hr" });
        Log(hrEmail, subject);
        return Task.CompletedTask;
    }

    private void Log(string to, string subject) =>
        logger.LogInformation("[EMAIL DEMO] Prima: {To} | Naslov: {Subject}", to, subject);
}
