using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record CodeCategories(IReadOnlyList<string> Categories);
public sealed record CodeValue(int Id, string Category, string Code, string Value, string? Description,
    int? DisplayOrder, bool IsActive, string? ModifiedBy, DateTime? ModifiedAt);
public sealed record CodeValues(string Category, IReadOnlyList<CodeValue> Items);
public sealed record CreateCodeValue(string Kategorija, string Kod, string Naziv, string? Opis, int? RedoslijedPrikaza, bool Aktivan);
public sealed record UpdateCodeValue(string Naziv, string? Opis, int? RedoslijedPrikaza, bool Aktivan);
public sealed record CodeImportPreview(int TotalRows, IReadOnlyList<string> ToImport, IReadOnlyList<string> Duplicates,
    IReadOnlyList<string> Errors, int Imported);
public interface ICodeListsApi
{
    Task<CodeCategories> CategoriesAsync(CancellationToken token = default);
    Task<CodeValues> ValuesAsync(string category, CancellationToken token = default);
    Task CreateCategoryAsync(string name, string? description, CancellationToken token = default);
    Task DeleteCategoryAsync(string category, CancellationToken token = default);
    Task CreateAsync(CreateCodeValue request, CancellationToken token = default);
    Task UpdateAsync(int id, UpdateCodeValue request, CancellationToken token = default);
    Task DeleteAsync(int id, CancellationToken token = default);
    Task<CodeImportPreview> ImportAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default);
}
public sealed class CodeListsApi(IRbiApiFileTransport transport) : ICodeListsApi
{
    public async Task<CodeCategories> CategoriesAsync(CancellationToken token = default)
    {
        var value = Value(await transport.GetAsync<CodeCategories>("api/code-lists", token), token);
        return value.Categories is null ? throw new RegistryApiException() : value;
    }
    public async Task<CodeValues> ValuesAsync(string category, CancellationToken token = default)
    {
        var value = Value(await transport.GetAsync<CodeValues>($"api/code-lists/{Uri.EscapeDataString(category)}", token), token);
        return value.Items is null ? throw new RegistryApiException() : value;
    }
    public Task CreateCategoryAsync(string name, string? description, CancellationToken token = default) =>
        SendAsync(HttpMethod.Post, "api/code-lists/categories", new { name, description }, token);
    public Task DeleteCategoryAsync(string category, CancellationToken token = default) =>
        SendAsync(HttpMethod.Delete, $"api/code-lists/categories/{Uri.EscapeDataString(category)}", null, token);
    public Task CreateAsync(CreateCodeValue request, CancellationToken token = default) => SendAsync(HttpMethod.Post, "api/code-lists", request, token);
    public Task UpdateAsync(int id, UpdateCodeValue request, CancellationToken token = default) => SendAsync(HttpMethod.Put, $"api/code-lists/{id}", request, token);
    public Task DeleteAsync(int id, CancellationToken token = default) => SendAsync(HttpMethod.Delete, $"api/code-lists/{id}", null, token);
    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken token) =>
        Value(await transport.SendAsync<JsonElement>(method, path, body, token), token);
    public async Task<CodeImportPreview> ImportAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default)
    {
        using var multipart = new MultipartFormDataContent();
        var content = new StreamContent(file); content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipart.Add(content, "file", fileName);
        var value = Value(await transport.PostMultipartAsync<CodeImportPreview>($"api/code-lists/{Uri.EscapeDataString(category)}/import?dryRun={dryRun.ToString().ToLowerInvariant()}", multipart, token), token);
        return value.ToImport is null || value.Duplicates is null || value.Errors is null ? throw new RegistryApiException() : value;
    }
    private static T Value<T>(RbiApiResult<T> result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredCodeListsApi : ICodeListsApi
{
    private static Task Unavailable() => Task.FromException(new RegistryApiException());
    public Task<CodeCategories> CategoriesAsync(CancellationToken token = default) => Task.FromException<CodeCategories>(new RegistryApiException());
    public Task<CodeValues> ValuesAsync(string category, CancellationToken token = default) => Task.FromException<CodeValues>(new RegistryApiException());
    public Task CreateCategoryAsync(string name, string? description, CancellationToken token = default) => Unavailable();
    public Task DeleteCategoryAsync(string category, CancellationToken token = default) => Unavailable();
    public Task CreateAsync(CreateCodeValue request, CancellationToken token = default) => Unavailable();
    public Task UpdateAsync(int id, UpdateCodeValue request, CancellationToken token = default) => Unavailable();
    public Task DeleteAsync(int id, CancellationToken token = default) => Unavailable();
    public Task<CodeImportPreview> ImportAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default)
    { file.Dispose(); return Task.FromException<CodeImportPreview>(new RegistryApiException()); }
}
