using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record CodeCategories(IReadOnlyList<string> Categories);
public sealed record CodeValue(int Id, string Category, string Code, string Value, string? Description,
    bool IsActive, string? ModifiedBy, DateTime? ModifiedAt, CodeListResource? Localization = null, IReadOnlyDictionary<int, JsonElement>? SlotValues = null);
public sealed record CodeListResource(string? LanguageCode, string? ResourceKey, string? ResourceLabel,
    string? ResourceHelper, string? ResourcePlaceholder, string? ContextInfo);
public sealed record CodeValues(string Category, IReadOnlyList<CodeValue> Items, IReadOnlyList<CodebookColumn>? Columns = null);
public sealed record CreateCodeValue(string Kategorija, string Kod, string Naziv, string? Opis, bool Aktivan);
public sealed record UpdateCodeValue(string Naziv, string? Opis, bool Aktivan);
public sealed record CodeCsvIssue(string Key, string[] Arguments);
public sealed record CodeCsvPreview(int TotalRows, int Ready, int Duplicates, int Imported, bool ExampleSkipped, IReadOnlyList<CodeCsvIssue> Errors);
public sealed record CodeImportPreview(int TotalRows, IReadOnlyList<string> ToImport, IReadOnlyList<string> Duplicates,
    IReadOnlyList<string> Errors, int Imported);
/// <summary>Administrative metadata of a code list definition (who created it and who changed it last).</summary>
public sealed record CodeListMeta(long Id, string Name, string? Description, bool IsActive, bool IsSystemRequired, int Version,
    DateTime? CreatedAt, string? CreatedBy, DateTime? ModifiedAt, string? ModifiedBy);
public sealed record CodebookColumn(int Slot, string Name, string? Description, int? Position, string TypeKey = "codes.csv.type-text");
public sealed record CodebookColumns(IReadOnlyList<CodebookColumn> Items);
public sealed record CodeListMetas(IReadOnlyList<CodeListMeta> Items);
public interface ICodeListsApi
{
    Task<CodebookColumns> ColumnsAsync(string category, CancellationToken token = default);
    Task<CodeListMetas> DefinitionsAsync(CancellationToken token = default);
    Task<CodeCategories> CategoriesAsync(CancellationToken token = default);
    Task<CodeValues> ValuesAsync(string category, CancellationToken token = default);
    Task CreateCategoryAsync(string name, string? description, CancellationToken token = default);
    Task DeleteCategoryAsync(string category, CancellationToken token = default);
    Task CreateAsync(CreateCodeValue request, CancellationToken token = default);
    Task UpdateAsync(int id, UpdateCodeValue request, CancellationToken token = default);
    Task DeleteAsync(int id, CancellationToken token = default);
    Task<RbiApiFile> CsvTemplateAsync(string category, CancellationToken token = default);
    Task<CodeCsvPreview> ImportCsvAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default);
    Task<CodeImportPreview> ImportAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default);
}
public sealed class CodeListsApi(IRbiApiFileTransport transport) : ICodeListsApi
{
    public async Task<CodeCategories> CategoriesAsync(CancellationToken token = default)
    {
        var value = Value(await transport.GetAsync<CodeCategories>("api/code-lists", token), token);
        return value.Categories is null ? throw new RegistryApiException() : value;
    }
    public async Task<CodebookColumns> ColumnsAsync(string category, CancellationToken token = default)
    {
        var value = Value(await transport.GetAsync<CodebookColumns>($"api/code-lists/categories/{Uri.EscapeDataString(category)}/columns", token), token);
        return value.Items is null ? throw new RegistryApiException() : value;
    }
    public async Task<CodeListMetas> DefinitionsAsync(CancellationToken token = default)
    {
        var value = Value(await transport.GetAsync<CodeListMetas>("api/code-lists/categories/definitions", token), token);
        return value.Items is null ? throw new RegistryApiException() : value;
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
    public async Task<RbiApiFile> CsvTemplateAsync(string category, CancellationToken token = default) =>
        Value(await transport.DownloadAsync($"api/code-lists/{Uri.EscapeDataString(category)}/csv-template", cancellationToken: token), token);
    public async Task<CodeCsvPreview> ImportCsvAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default)
    {
        using var multipart = new MultipartFormDataContent();
        var content = new StreamContent(file);
        content.Headers.ContentType = new("text/csv");
        multipart.Add(content, "file", fileName);
        return Value(await transport.PostMultipartAsync<CodeCsvPreview>(
            $"api/code-lists/{Uri.EscapeDataString(category)}/csv-import?dryRun={dryRun.ToString().ToLowerInvariant()}", multipart, token), token);
    }
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
    public Task<CodebookColumns> ColumnsAsync(string category, CancellationToken token = default) => Task.FromException<CodebookColumns>(new RegistryApiException());
    public Task<CodeListMetas> DefinitionsAsync(CancellationToken token = default) => Task.FromException<CodeListMetas>(new RegistryApiException());
    public Task<CodeValues> ValuesAsync(string category, CancellationToken token = default) => Task.FromException<CodeValues>(new RegistryApiException());
    public Task CreateCategoryAsync(string name, string? description, CancellationToken token = default) => Unavailable();
    public Task DeleteCategoryAsync(string category, CancellationToken token = default) => Unavailable();
    public Task CreateAsync(CreateCodeValue request, CancellationToken token = default) => Unavailable();
    public Task UpdateAsync(int id, UpdateCodeValue request, CancellationToken token = default) => Unavailable();
    public Task DeleteAsync(int id, CancellationToken token = default) => Unavailable();
    public Task<RbiApiFile> CsvTemplateAsync(string category, CancellationToken token = default) => Task.FromException<RbiApiFile>(new RegistryApiException());
    public Task<CodeCsvPreview> ImportCsvAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default) => Task.FromException<CodeCsvPreview>(new RegistryApiException());
    public Task<CodeImportPreview> ImportAsync(string category, Stream file, string fileName, bool dryRun, CancellationToken token = default)
    { file.Dispose(); return Task.FromException<CodeImportPreview>(new RegistryApiException()); }
}
