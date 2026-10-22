using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

// Frontend wire contracts only. The separate backend owns persistence and
// authorization; no entities/DbContext/repositories belong in this application.
public sealed record AuditLogEntry(string Id, DateTimeOffset Timestamp, string User,
    string Action, string Resource, string Result, string? Details = null, string? CorrelationId = null);

public sealed record AuditLogPage(IReadOnlyList<AuditLogEntry> Items, int TotalCount, int Page = 0, int PageSize = 0);

public interface IAuditLogApi
{
    Task<AuditLogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
}

internal sealed class EmptyAuditLogApi : IAuditLogApi
{
    public Task<AuditLogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(page));
        return Task.FromResult(new AuditLogPage([], 0, page, pageSize));
    }
}

internal sealed class UnavailableAuditLogApi : IAuditLogApi
{
    public Task<AuditLogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
        => Task.FromException<AuditLogPage>(new AuditLogUnavailableException());
}

internal sealed class AuditLogApi(IRbiApiTransport transport) : IAuditLogApi
{
    public async Task<AuditLogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(page));
        var result = await transport.GetAsync<AuditLogPage>(
            $"api/audit?page={page}&pageSize={pageSize}", cancellationToken);
        if (!result.IsSuccess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Upstream Title/Detail may be sensitive; UI selects its own wording.
            throw new AuditLogUnavailableException();
        }
        var response = result.Value;
        if (response.Items is null || response.Page != page || response.PageSize != pageSize ||
            response.TotalCount < 0 || response.Items.Count > pageSize ||
            response.TotalCount < response.Items.Count ||
            response.Items.Any(entry => entry is null || string.IsNullOrWhiteSpace(entry.Id) ||
                entry.Timestamp == default || string.IsNullOrWhiteSpace(entry.User) ||
                string.IsNullOrWhiteSpace(entry.Action) || string.IsNullOrWhiteSpace(entry.Resource) ||
                string.IsNullOrWhiteSpace(entry.Result)) ||
            response.Items.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count() != response.Items.Count)
            throw new AuditLogUnavailableException();
        return response;
    }
}

public sealed class AuditLogUnavailableException : Exception
{
    public AuditLogUnavailableException() : base("Audit service is unavailable.") { }
}
