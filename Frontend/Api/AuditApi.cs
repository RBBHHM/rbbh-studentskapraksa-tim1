using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record AuditRecord(long Id, string TableName, string Action, string ActionDisplay,
    string AreaDisplay, string ChangeSummary, string? OldValues, string? NewValues,
    string Username, DateTime Timestamp)
{
    public bool Matches(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        string?[] values = [Id.ToString(), TableName, Action, ActionDisplay, AreaDisplay,
            ChangeSummary, OldValues, NewValues, Username, Timestamp.ToString("O")];
        return values.Any(value => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
    }
}
public sealed record AuditPage(IReadOnlyList<AuditRecord> Logs, int Total, int Page, int PageSize, IReadOnlyDictionary<long, string>? PermissionCodes = null,
    IReadOnlyDictionary<string, Dictionary<long, string>>? ReferenceNames = null);
public interface IAuditApi { Task<AuditPage> GetAsync(int page = 1, CancellationToken token = default, string? search = null); }
public sealed class AuditApi(IRbiApiTransport transport) : IAuditApi
{
    public async Task<AuditPage> GetAsync(int page = 1, CancellationToken token = default, string? search = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var result = await transport.GetAsync<AuditPage>($"api/audit-logs?page={page}&pageSize=50" + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search.Trim())}"), token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredAuditApi : IAuditApi
{
    public Task<AuditPage> GetAsync(int page = 1, CancellationToken token = default, string? search = null) => Task.FromException<AuditPage>(new RegistryApiException());
}
