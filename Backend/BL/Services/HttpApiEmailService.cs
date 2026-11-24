using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Email;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class HttpApiEmailService(
    HttpClient httpClient,
    IOptions<EmailSettings> options,
    ILogger<HttpApiEmailService> logger) : IEmailService
{
    public async Task SendApplicationNotificationAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var email = new EmailDto { From = settings.From, To = to, Subject = subject, Text = htmlBody };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(settings.SendEmailWithHtmlBodyUrl, email, cancellationToken);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Email delivered through the configured HTTP transport.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("Email transport failed. Status {StatusCode}", exception.StatusCode);
            throw;
        }
    }
}
