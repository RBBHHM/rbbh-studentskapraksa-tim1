using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

/// <summary>Recipient-scoped inbox item. Keys and parameters are localized by the UI; the route is an authenticated relative path.</summary>
public sealed record InboxNotification(Guid Id, string TitleKey, string BodyKey, JsonElement Parameters, string? Route, DateTime CreatedAt, DateTime? ReadAt)
{
    public bool IsUnread => ReadAt is null;
}
public sealed record InboxPage(IReadOnlyList<InboxNotification> Items, int Total, int Page, int PageSize, int UnreadCount);
public interface INotificationsApi
{
    Task<InboxPage> InboxAsync(int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken token = default);
    Task MarkReadAsync(Guid id, CancellationToken token = default);
}
public sealed class NotificationsApi(IRbiApiTransport transport) : INotificationsApi
{
    public async Task<InboxPage> InboxAsync(int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        var result = await transport.GetAsync<InboxPage>($"api/notifications?page={page}&pageSize={pageSize}&unreadOnly={unreadOnly.ToString().ToLowerInvariant()}", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task MarkReadAsync(Guid id, CancellationToken token = default)
    {
        var result = await transport.SendAsync(HttpMethod.Post, $"api/notifications/{id}/read", null, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
    }
}
internal sealed class UnconfiguredNotificationsApi : INotificationsApi
{
    public Task<InboxPage> InboxAsync(int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken token = default) => Task.FromException<InboxPage>(new RegistryApiException());
    public Task MarkReadAsync(Guid id, CancellationToken token = default) => Task.FromException(new RegistryApiException());
}
