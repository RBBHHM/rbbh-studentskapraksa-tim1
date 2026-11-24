using RBBH.ConnectedParties.BL.ServiceInterfaces;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class MockEmailService(EmailLogStore store, ILogger<MockEmailService> logger) : IEmailService
{
    public Task SendApplicationNotificationAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        store.Add(new EmailLogEntry { To = to, Subject = subject, HtmlBody = htmlBody, Audience = "application" });
        logger.LogInformation("Recorded demo email.");
        return Task.CompletedTask;
    }
}
