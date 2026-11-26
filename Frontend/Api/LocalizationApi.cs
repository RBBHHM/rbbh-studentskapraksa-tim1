using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RuntimeResource(string ResourceKey, string Label, string? Helper, string? Placeholder, string? ContextInfo = null);
public sealed record RuntimeResourcePack(string Language, string? FallbackLanguage, string Version, IReadOnlyList<RuntimeResource> Items);
public sealed record RuntimeLanguage(string Code, string Name);
public sealed record PutRuntimeResource(string Label, string? Helper, string? Placeholder, bool IsActive, string? ContextInfo);
public sealed record RuntimeLanguages(IReadOnlyList<RuntimeLanguage> Items);
public interface ILocalizationApi
{
    Task<string?> SaveAsync(string language, string key, PutRuntimeResource resource, CancellationToken token = default);
    Task<RuntimeLanguages> LanguagesAsync(CancellationToken token = default);
    Task<RuntimeResourcePack> ResourcesAsync(string language, CancellationToken token = default);
}
public sealed class LocalizationApi(IRbiApiTransport transport) : ILocalizationApi
{
    public async Task<string?> SaveAsync(string language, string key, PutRuntimeResource resource, CancellationToken token = default)
    {
        var result = await transport.SendAsync(HttpMethod.Put, $"api/localization/resources/{Uri.EscapeDataString(language)}/{Uri.EscapeDataString(key)}", resource, token);
        token.ThrowIfCancellationRequested();
        return result.IsSuccess ? null : new RegistryApiException(result.Error?.StatusCode, result.Error?.Code).MessageKey;
    }
    public async Task<RuntimeLanguages> LanguagesAsync(CancellationToken token = default)
    {
        var result = await transport.GetAsync<RuntimeLanguages>("api/localization/languages", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task<RuntimeResourcePack> ResourcesAsync(string language, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        var result = await transport.GetAsync<RuntimeResourcePack>($"api/localization/resources/{Uri.EscapeDataString(language)}", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}
internal sealed class UnconfiguredLocalizationApi : ILocalizationApi
{
    public Task<string?> SaveAsync(string language, string key, PutRuntimeResource resource, CancellationToken token = default) => Task.FromResult<string?>("errors.unavailable");
    public Task<RuntimeLanguages> LanguagesAsync(CancellationToken token = default) => Task.FromException<RuntimeLanguages>(new RegistryApiException());
    public Task<RuntimeResourcePack> ResourcesAsync(string language, CancellationToken token = default) => Task.FromException<RuntimeResourcePack>(new RegistryApiException());
}
