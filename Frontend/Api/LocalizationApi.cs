using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RuntimeResource(string ResourceKey, string Label, string? Helper, string? Placeholder);
public sealed record RuntimeResourcePack(string Language, string? FallbackLanguage, string Version, IReadOnlyList<RuntimeResource> Items);
public sealed record RuntimeLanguage(string Code, string Name);
public sealed record RuntimeLanguages(IReadOnlyList<RuntimeLanguage> Items);
public interface ILocalizationApi
{
    Task<RuntimeLanguages> LanguagesAsync(CancellationToken token = default);
    Task<RuntimeResourcePack> ResourcesAsync(string language, CancellationToken token = default);
}
public sealed class LocalizationApi(IRbiApiTransport transport) : ILocalizationApi
{
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
    public Task<RuntimeLanguages> LanguagesAsync(CancellationToken token = default) => Task.FromException<RuntimeLanguages>(new RegistryApiException());
    public Task<RuntimeResourcePack> ResourcesAsync(string language, CancellationToken token = default) => Task.FromException<RuntimeResourcePack>(new RegistryApiException());
}
