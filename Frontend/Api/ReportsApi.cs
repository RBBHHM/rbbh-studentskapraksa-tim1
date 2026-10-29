using System.Text.RegularExpressions;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public enum ReportKind { Daily, Monthly }
public sealed record ReportEntry(Guid Id, string ReportType, DateTime ReportDate, int TotalClients,
    int ClientsWithBreachedLimit, decimal TotalExposure, string CreatedBy, DateTime CreatedAt);
public sealed record ReportArchive(IReadOnlyList<ReportEntry> Items, int Total, int Page, int PageSize);
public interface IReportsApi
{
    Task<ReportArchive> ListAsync(ReportKind kind, int page = 1, CancellationToken token = default);
    Task<ReportEntry> GenerateDailyAsync(CancellationToken token = default);
    Task<ReportEntry> GenerateMonthlyAsync(int year, int month, CancellationToken token = default);
    Task<RbiApiFile> DownloadAsync(Guid id, CancellationToken token = default);
    Task<RbiApiFile> ExportAllAsync(CancellationToken token = default);
    Task<RbiApiFile> ExportClientAsync(string identifier, CancellationToken token = default);
}
public sealed class ReportsApi(IRbiApiFileTransport transport) : IReportsApi
{
    public async Task<ReportArchive> ListAsync(ReportKind kind, int page = 1, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var path = kind switch { ReportKind.Daily => "daily", ReportKind.Monthly => "monthly", _ => throw new ArgumentOutOfRangeException(nameof(kind)) };
        return Value(await transport.GetAsync<ReportArchive>($"api/reports/{path}?page={page}&pageSize=100", token), token);
    }
    public async Task<ReportEntry> GenerateDailyAsync(CancellationToken token = default) =>
        Value(await transport.SendAsync<ReportEntry>(HttpMethod.Post, "api/reports/daily", cancellationToken: token), token);
    public async Task<ReportEntry> GenerateMonthlyAsync(int year, int month, CancellationToken token = default)
    {
        if (year < 2000 || year > DateTime.UtcNow.Year) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        return Value(await transport.SendAsync<ReportEntry>(HttpMethod.Post, $"api/reports/monthly/{year}/{month}", cancellationToken: token), token);
    }
    public async Task<RbiApiFile> DownloadAsync(Guid id, CancellationToken token = default) =>
        Value(await transport.DownloadAsync($"api/reports/export/generated/{id:D}", cancellationToken: token), token);
    public async Task<RbiApiFile> ExportAllAsync(CancellationToken token = default) =>
        Value(await transport.DownloadAsync("api/reports/export/all-clients", cancellationToken: token), token);
    public async Task<RbiApiFile> ExportClientAsync(string identifier, CancellationToken token = default)
    {
        if (!ValidIdentifier(identifier)) throw new ArgumentException("Use the existing numeric tax/FBA identifier contract.", nameof(identifier));
        return Value(await transport.DownloadAsync("api/reports/export/client/" + Uri.EscapeDataString(identifier.Trim()), cancellationToken: token), token);
    }
    public static bool ValidIdentifier(string? value) => Regex.IsMatch(value?.Trim() ?? "", "^[0-9]{1,13}$");
    private static T Value<T>(RbiApiResult<T> result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredReportsApi : IReportsApi
{
    private static Task<T> Unavailable<T>() => Task.FromException<T>(new RegistryApiException());
    public Task<ReportArchive> ListAsync(ReportKind kind, int page = 1, CancellationToken token = default) => Unavailable<ReportArchive>();
    public Task<ReportEntry> GenerateDailyAsync(CancellationToken token = default) => Unavailable<ReportEntry>();
    public Task<ReportEntry> GenerateMonthlyAsync(int year, int month, CancellationToken token = default) => Unavailable<ReportEntry>();
    public Task<RbiApiFile> DownloadAsync(Guid id, CancellationToken token = default) => Unavailable<RbiApiFile>();
    public Task<RbiApiFile> ExportAllAsync(CancellationToken token = default) => Unavailable<RbiApiFile>();
    public Task<RbiApiFile> ExportClientAsync(string identifier, CancellationToken token = default) => Unavailable<RbiApiFile>();
}
