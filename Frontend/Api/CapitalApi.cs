using System.Net;
using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record CapitalRecord(Guid Id, decimal RegulatoryCapital, decimal CoreCapital,
    decimal SupplementaryCapital, DateTime CapitalDate);
public sealed record CapitalRequest(decimal RegulatoryCapital, decimal CoreCapital,
    decimal SupplementaryCapital, DateTime CapitalDate);
/// <summary>One immutable effective-date observation (source "revision", or "legacy" for the pre-history row).</summary>
public sealed record CapitalVersion(Guid Id, long Revision, DateTime EffectiveDate, decimal RegulatoryCapital, decimal CoreCapital,
    decimal SupplementaryCapital, DateTime RecordedAt, string RecordedBy, string Source);
public sealed record CapitalHistoryPage(IReadOnlyList<CapitalVersion> Items, int Total, int Page, int PageSize);
public interface ICapitalApi
{
    Task<CapitalHistoryPage> HistoryAsync(int page = 1, int pageSize = 100, CancellationToken token = default);
    /// <summary>Capital valid on the date (latest effective date not after it); null when none was recorded.</summary>
    Task<CapitalRecord?> AsOfAsync(DateOnly date, CancellationToken token = default);
    Task<CapitalRecord?> GetAsync(CancellationToken token = default);
    Task<CapitalRecord> SaveAsync(CapitalRequest request, CancellationToken token = default);
    Task<RbiApiFile> ExportAsync(CancellationToken token = default);
}
public sealed class CapitalApi(IRbiApiFileTransport transport) : ICapitalApi
{
    private static readonly JsonSerializerOptions Contract = new(JsonSerializerDefaults.Web) { RespectRequiredConstructorParameters = true };
    public async Task<CapitalHistoryPage> HistoryAsync(int page = 1, int pageSize = 100, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        var result = await transport.GetAsync<CapitalHistoryPage>($"api/capital/history?page={page}&pageSize={pageSize}", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public Task<CapitalRecord?> AsOfAsync(DateOnly date, CancellationToken token = default) =>
        ReadOptionalAsync($"api/capital/as-of?date={date:yyyy-MM-dd}", token);
    public Task<CapitalRecord?> GetAsync(CancellationToken token = default) => ReadOptionalAsync("api/capital", token);
    private async Task<CapitalRecord?> ReadOptionalAsync(string path, CancellationToken token)
    {
        // This endpoint deliberately has an optional singleton. ASP.NET can emit
        // 204 for Ok(null); a literal JSON null is also part of its declared contract.
        // Do not relax required-value behavior in the shared transport or normalize
        // errors/malformed JSON/404 into a fictional zero-valued capital record.
        var result = await transport.GetAsync<JsonElement>(path, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess)
        {
            if (result.Error is { Kind: RbiApiErrorKind.Unknown, StatusCode: HttpStatusCode.NoContent }) return null;
            throw new RegistryApiException(result.Error?.StatusCode);
        }
        if (result.Value.ValueKind == JsonValueKind.Null) return null;
        try
        {
            return result.Value.ValueKind == JsonValueKind.Object
                ? result.Value.Deserialize<CapitalRecord>(Contract) ?? throw new RegistryApiException()
                : throw new RegistryApiException();
        }
        catch (JsonException) { throw new RegistryApiException(); }
    }
    public async Task<CapitalRecord> SaveAsync(CapitalRequest request, CancellationToken token = default)
    {
        var result = await transport.SendAsync<CapitalRecord>(HttpMethod.Put, "api/capital", request, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task<RbiApiFile> ExportAsync(CancellationToken token = default)
    {
        var result = await transport.DownloadAsync("api/capital/export", cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredCapitalApi : ICapitalApi
{
    public Task<CapitalHistoryPage> HistoryAsync(int page = 1, int pageSize = 100, CancellationToken token = default) => Task.FromException<CapitalHistoryPage>(new RegistryApiException());
    public Task<CapitalRecord?> AsOfAsync(DateOnly date, CancellationToken token = default) => Task.FromException<CapitalRecord?>(new RegistryApiException());
    public Task<CapitalRecord?> GetAsync(CancellationToken token = default) => Task.FromException<CapitalRecord?>(new RegistryApiException());
    public Task<CapitalRecord> SaveAsync(CapitalRequest request, CancellationToken token = default) => Task.FromException<CapitalRecord>(new RegistryApiException());
    public Task<RbiApiFile> ExportAsync(CancellationToken token = default) => Task.FromException<RbiApiFile>(new RegistryApiException());
}
