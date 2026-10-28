using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record LimitRecord(int Id, Guid LegalEntityId, string Naziv, bool IsResident, string? FbaId,
    string? TaxNumber, string? MaticniBroj, string? Matbroj, string TipLimita, decimal? OdobreniLimit,
    decimal? MaksimalnaOcekivanaUtilizacija, DateTime? RokOcekivaneUtilizacije, string? Komentar,
    decimal? RegulatorniKapital, decimal? OsnovniKapital, DateTime? DatumKapitala,
    DateTime CreatedAt, string CreatedBy, DateTime? ModifiedAt, string? ModifiedBy)
{
    public bool Matches(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        object?[] values = [Id, LegalEntityId, Naziv, IsResident, FbaId, TaxNumber, MaticniBroj, Matbroj,
            TipLimita, OdobreniLimit, MaksimalnaOcekivanaUtilizacija, RokOcekivaneUtilizacije?.ToString("O"), Komentar,
            RegulatorniKapital, OsnovniKapital, DatumKapitala?.ToString("O"), CreatedAt.ToString("O"), CreatedBy, ModifiedAt?.ToString("O"), ModifiedBy];
        return values.Any(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
    }
}
public sealed record LimitRequest(Guid LegalEntityId, string TipLimita, decimal? OdobreniLimit,
    decimal? MaksimalnaOcekivanaUtilizacija, DateTime? RokOcekivaneUtilizacije, string? Komentar);
public sealed record LimitEntityOption(Guid Id, string Name, string? MaticniBroj, string? TaxNumber);
public sealed record LimitEntityData(Guid Id, bool IsResident, string? FbaId, string? TaxNumber,
    string? MaticniBroj, string? Matbroj, string Name, decimal? RegulatoryCapital, decimal? CoreCapital);

public interface ILimitsApi
{
    Task<IReadOnlyList<LimitRecord>> ListAsync(Guid? entity = null, CancellationToken token = default);
    Task<LimitRecord> GetAsync(int id, CancellationToken token = default);
    Task<IReadOnlyList<LimitEntityOption>> SearchEntitiesAsync(string? search, CancellationToken token = default);
    Task<LimitEntityData> EntityDataAsync(Guid id, CancellationToken token = default);
    Task<LimitRecord> SaveAsync(int? id, LimitRequest request, CancellationToken token = default);
    Task DeleteAsync(int id, CancellationToken token = default);
    Task<RbiApiFile> ExportAsync(Guid? entity = null, CancellationToken token = default);
}
public sealed class LimitsApi(IRbiApiFileTransport transport) : ILimitsApi
{
    private static string Filter(Guid? id) => id is { } key ? $"?legalEntityId={key:D}" : "";
    public async Task<IReadOnlyList<LimitRecord>> ListAsync(Guid? entity = null, CancellationToken token = default) =>
        Value(await transport.GetAsync<List<LimitRecord>>("api/limiti" + Filter(entity), token), token);
    public async Task<LimitRecord> GetAsync(int id, CancellationToken token = default) =>
        Value(await transport.GetAsync<LimitRecord>($"api/limiti/{id}", token), token);
    public async Task<IReadOnlyList<LimitEntityOption>> SearchEntitiesAsync(string? search, CancellationToken token = default) =>
        Value(await transport.GetAsync<List<LimitEntityOption>>("api/legal-entities/limits/search?search=" + Uri.EscapeDataString(search ?? ""), token), token);
    public async Task<LimitEntityData> EntityDataAsync(Guid id, CancellationToken token = default) =>
        Value(await transport.GetAsync<LimitEntityData>($"api/legal-entities/{id:D}/limit-form-data", token), token);
    public async Task<LimitRecord> SaveAsync(int? id, LimitRequest request, CancellationToken token = default) =>
        Value(await transport.SendAsync<LimitRecord>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            "api/limiti" + (id is { } key ? $"/{key}" : ""), request, token), token);
    public async Task DeleteAsync(int id, CancellationToken token = default)
    {
        var result = await transport.SendAsync(HttpMethod.Delete, $"api/limiti/{id}", cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
    }
    public async Task<RbiApiFile> ExportAsync(Guid? entity = null, CancellationToken token = default) =>
        Value(await transport.DownloadAsync("api/limiti/export" + Filter(entity), cancellationToken: token), token);
    private static T Value<T>(RbiApiResult<T> result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredLimitsApi : ILimitsApi
{
    private static Task<T> Unavailable<T>() => Task.FromException<T>(new RegistryApiException());
    public Task<IReadOnlyList<LimitRecord>> ListAsync(Guid? entity = null, CancellationToken token = default) => Unavailable<IReadOnlyList<LimitRecord>>();
    public Task<LimitRecord> GetAsync(int id, CancellationToken token = default) => Unavailable<LimitRecord>();
    public Task<IReadOnlyList<LimitEntityOption>> SearchEntitiesAsync(string? search, CancellationToken token = default) => Unavailable<IReadOnlyList<LimitEntityOption>>();
    public Task<LimitEntityData> EntityDataAsync(Guid id, CancellationToken token = default) => Unavailable<LimitEntityData>();
    public Task<LimitRecord> SaveAsync(int? id, LimitRequest request, CancellationToken token = default) => Unavailable<LimitRecord>();
    public Task DeleteAsync(int id, CancellationToken token = default) => Unavailable<object>();
    public Task<RbiApiFile> ExportAsync(Guid? entity = null, CancellationToken token = default) => Unavailable<RbiApiFile>();
}
