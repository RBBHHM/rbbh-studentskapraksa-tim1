using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record PeriodState(int Year, int Month, string MonthDisplay, bool IsLocked, string? LockedBy, DateTime? LockedAt);
public sealed record UnlockRequestRecord(Guid Id, string RequestedBy, int Year, int Month, string MonthDisplay,
    string Reason, string Status, string? AdminNote, string? ProcessedBy, DateTime? ProcessedAt, DateTime CreatedAt);
public sealed record UnlockRequestsPage(IReadOnlyList<UnlockRequestRecord> Requests, int Total, int Page, int PageSize);
public interface IPeriodApi
{
    Task<PeriodState> CurrentAsync(CancellationToken token = default);
    Task<UnlockRequestsPage> RequestsAsync(string status = "", int page = 1, CancellationToken token = default);
    Task SetLockedAsync(bool locked, int? year = null, int? month = null, CancellationToken token = default);
    Task RequestUnlockAsync(int year, int month, string reason, CancellationToken token = default);
    Task ProcessAsync(Guid id, bool reject, string note, CancellationToken token = default);
    Task RespondAsync(Guid id, string message, CancellationToken token = default);
}
public sealed class PeriodApi(IRbiApiTransport transport) : IPeriodApi
{
    public async Task<PeriodState> CurrentAsync(CancellationToken token = default)
    {
        var result = await transport.GetAsync<PeriodState>("api/period-lock/current", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task<UnlockRequestsPage> RequestsAsync(string status = "", int page = 1, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var result = await transport.GetAsync<UnlockRequestsPage>($"api/period-lock/unlock-requests?status={Uri.EscapeDataString(status)}&page={page}&pageSize=100", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Requests is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public Task SetLockedAsync(bool locked, int? year = null, int? month = null, CancellationToken token = default) =>
        SendAsync(locked ? "lock" : "unlock", new { year, month }, token);
    public Task RequestUnlockAsync(int year, int month, string reason, CancellationToken token = default) =>
        SendAsync("request-unlock", new { year, month, reason }, token);
    public Task ProcessAsync(Guid id, bool reject, string note, CancellationToken token = default) =>
        SendAsync($"unlock-requests/{id}/{(reject ? "reject" : "request-info")}", new { note }, token);
    public Task RespondAsync(Guid id, string message, CancellationToken token = default) =>
        SendAsync($"unlock-requests/{id}/respond", new { message }, token);
    private async Task SendAsync(string path, object body, CancellationToken token)
    {
        var result = await transport.SendAsync<JsonElement>(HttpMethod.Post, "api/period-lock/" + path, body, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
    }
}
internal sealed class UnconfiguredPeriodApi : IPeriodApi
{
    public Task<PeriodState> CurrentAsync(CancellationToken token = default) => Task.FromException<PeriodState>(new RegistryApiException());
    public Task<UnlockRequestsPage> RequestsAsync(string status = "", int page = 1, CancellationToken token = default) => Task.FromException<UnlockRequestsPage>(new RegistryApiException());
    public Task SetLockedAsync(bool locked, int? year = null, int? month = null, CancellationToken token = default) => Task.FromException(new RegistryApiException());
    public Task RequestUnlockAsync(int year, int month, string reason, CancellationToken token = default) => Task.FromException(new RegistryApiException());
    public Task ProcessAsync(Guid id, bool reject, string note, CancellationToken token = default) => Task.FromException(new RegistryApiException());
    public Task RespondAsync(Guid id, string message, CancellationToken token = default) => Task.FromException(new RegistryApiException());
}
