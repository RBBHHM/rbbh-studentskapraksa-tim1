using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record AuditRecord(Guid Id, string TableName, string RecordId, string Action, string ActionDisplay,
    string AreaDisplay, string ChangeSummary, string? OldValues, string? NewValues,
    string Username, string? IpAddress, DateTime Timestamp)
{
    public bool Matches(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        string?[] values = [Id.ToString(), TableName, RecordId, Action, ActionDisplay, AreaDisplay,
            ChangeSummary, OldValues, NewValues, Username, IpAddress, Timestamp.ToString("O")];
        return values.Any(value => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
    }
}
public sealed record AuditPage(IReadOnlyList<AuditRecord> Logs, int Total, int Page, int PageSize);
public interface IAuditApi { Task<AuditPage> GetAsync(int page = 1, CancellationToken token = default); }
public sealed class AuditApi(IRbiApiTransport transport) : IAuditApi
{
    public async Task<AuditPage> GetAsync(int page = 1, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var result = await transport.GetAsync<AuditPage>($"api/audit-logs?page={page}&pageSize=200", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredAuditApi : IAuditApi
{
    public Task<AuditPage> GetAsync(int page = 1, CancellationToken token = default) => Task.FromException<AuditPage>(new RegistryApiException());
}
