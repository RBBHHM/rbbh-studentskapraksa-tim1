using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RecordedNotification(Guid Id, string To, string Subject, string HtmlBody,
    DateTime SentAt, string Audience, string Purpose, string DeliveryStatus, Guid? RequestId)
{
    // Preserve source scalar search, including recorded body text. HTML is never
    // rendered as markup, sent to a rich-text component or executed by the UI.
    public bool Matches(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        string?[] values = [Id.ToString(), To, Subject, HtmlBody, SentAt.ToString("O"), Audience, Purpose, DeliveryStatus, RequestId?.ToString()];
        return values.Any(value => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
    }
}
public sealed record NotificationLog(string Provider, IReadOnlyList<RecordedNotification> Emails);
public interface INotificationsApi { Task<NotificationLog> GetAsync(CancellationToken token = default); }
public sealed class NotificationsApi(IRbiApiTransport transport) : INotificationsApi
{
    public async Task<NotificationLog> GetAsync(CancellationToken token = default)
    {
        var result = await transport.GetAsync<NotificationLog>("api/email-log", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredNotificationsApi : INotificationsApi
{
    public Task<NotificationLog> GetAsync(CancellationToken token = default) => Task.FromException<NotificationLog>(new RegistryApiException());
}
