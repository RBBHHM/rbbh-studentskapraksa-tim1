using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record LegalEntity(Guid Id, bool IsResident, string? TaxNumber, string? MaticniBroj,
    string? FbaId, string Name, string? Matbroj, string BasisOfConnection, string? ConnectionDescription,
    bool? ConnectedWithBank, DateTime? DateFrom, DateTime? DateTo, string Status,
    string CreatedBy, DateTime CreatedAt, string? ModifiedBy, DateTime? ModifiedAt,
    string? VerifiedBy, DateTime? VerifiedAt)
{
    // Existing UI searches scalar response metadata too. Keep that behavior
    // explicit; the narrower server search is not a drop-in replacement.
    public bool Matches(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        object?[] values = [Id, IsResident, TaxNumber, MaticniBroj, FbaId, Name, Matbroj,
            BasisOfConnection, ConnectionDescription, ConnectedWithBank, DateFrom?.ToString("O"),
            DateTo?.ToString("O"), Status, CreatedBy, CreatedAt.ToString("O"), ModifiedBy,
            ModifiedAt?.ToString("O"), VerifiedBy, VerifiedAt?.ToString("O")];
        return values.Any(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
    }
}
public sealed record LegalEntityPage(IReadOnlyList<LegalEntity> Items, int Total, int Page, int PageSize);
public sealed record LegalEntityRequest(bool IsResident, string? TaxNumber, string? MaticniBroj,
    string? FbaId, string Name, string? Matbroj, string BasisOfConnection, string? ConnectionDescription,
    bool? ConnectedWithBank, DateTime? DateFrom, DateTime? DateTo);

public interface ILegalEntitiesApi
{
    Task<LegalEntityPage> ListAsync(int page = 1, CancellationToken token = default);
    Task<LegalEntity> GetAsync(Guid id, CancellationToken token = default);
    Task<LegalEntity> SaveAsync(Guid? id, LegalEntityRequest request, CancellationToken token = default);
    Task VerifyAsync(Guid id, CancellationToken token = default);
    Task DeleteAsync(Guid id, CancellationToken token = default);
    Task<RbiApiFile> ExportAsync(CancellationToken token = default);
    Task<ImportResult> ImportAsync(Stream file, string name, CancellationToken token = default);
}

public sealed class LegalEntitiesApi(IRbiApiFileTransport transport) : ILegalEntitiesApi
{
    public async Task<LegalEntityPage> ListAsync(int page = 1, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        return Value(await transport.GetAsync<LegalEntityPage>($"api/legal-entities?page={page}&pageSize=20", token), token);
    }
    public async Task<LegalEntity> GetAsync(Guid id, CancellationToken token = default) =>
        Value(await transport.GetAsync<LegalEntity>($"api/legal-entities/{id:D}", token), token);
    public async Task<LegalEntity> SaveAsync(Guid? id, LegalEntityRequest request, CancellationToken token = default) =>
        Value(await transport.SendAsync<LegalEntity>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            "api/legal-entities" + (id is { } key ? $"/{key:D}" : ""), request, token), token);
    public async Task VerifyAsync(Guid id, CancellationToken token = default) =>
        Value(await transport.SendAsync<LegalEntity>(HttpMethod.Post, $"api/verification/legal-person/{id:D}", cancellationToken: token), token);
    public async Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        var result = await transport.SendAsync(HttpMethod.Delete, $"api/legal-entities/{id:D}", cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
    }
    public async Task<RbiApiFile> ExportAsync(CancellationToken token = default) =>
        Value(await transport.DownloadAsync("api/legal-entities/export", cancellationToken: token), token);
    public async Task<ImportResult> ImportAsync(Stream file, string name, CancellationToken token = default)
    {
        using var multipart = new MultipartFormDataContent();
        var content = new StreamContent(file);
        content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipart.Add(content, "file", name);
        return Value(await transport.PostMultipartAsync<ImportResult>("api/legal-entities/import", multipart, token), token);
    }
    private static T Value<T>(RbiApiResult<T> result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredLegalEntitiesApi : ILegalEntitiesApi
{
    private static Task<T> Unavailable<T>() => Task.FromException<T>(new RegistryApiException());
    public Task<LegalEntityPage> ListAsync(int page = 1, CancellationToken token = default) => Unavailable<LegalEntityPage>();
    public Task<LegalEntity> GetAsync(Guid id, CancellationToken token = default) => Unavailable<LegalEntity>();
    public Task<LegalEntity> SaveAsync(Guid? id, LegalEntityRequest request, CancellationToken token = default) => Unavailable<LegalEntity>();
    public Task VerifyAsync(Guid id, CancellationToken token = default) => Unavailable<object>();
    public Task DeleteAsync(Guid id, CancellationToken token = default) => Unavailable<object>();
    public Task<RbiApiFile> ExportAsync(CancellationToken token = default) => Unavailable<RbiApiFile>();
    public Task<ImportResult> ImportAsync(Stream file, string name, CancellationToken token = default) { file.Dispose(); return Unavailable<ImportResult>(); }
}
