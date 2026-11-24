namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IEmailService
{
    Task SendApplicationNotificationAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
