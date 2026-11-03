using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class RuntimeLocalizationOptions
{
    public string LanguagesCodebook { get; set; } = "Languages";
    public string LanguagePacksCodebook { get; set; } = "LanguagePacks";
    public string DefaultLanguage { get; set; } = "BS";
}

public sealed record RuntimeResource(string ResourceKey, string Label, string? Helper, string? Placeholder);
public sealed record RuntimeResourcePack(string Language, string FallbackLanguage, string Version, IReadOnlyList<RuntimeResource> Items);
public sealed record PutRuntimeResource(string Label, string? Helper = null, string? Placeholder = null, bool IsActive = true);

public sealed class RuntimeLocalizationCache : IDisposable
{
    internal MemoryCache Values { get; } = new(new MemoryCacheOptions { SizeLimit = 50_000 });
    public void Dispose() => Values.Dispose();
}

/// <summary>Runtime values in the application's existing codebook area, not a second frontend localization engine.</summary>
public sealed class RuntimeLocalizationCatalog(ConnectedPartiesDbContext db, IOptions<RuntimeLocalizationOptions> options,
    RuntimeLocalizationCache cache, ILogger<RuntimeLocalizationCatalog> logger)
{
    private RuntimeLocalizationOptions Settings => options.Value;
    public string DefaultLanguage => Settings.DefaultLanguage.Trim().ToUpperInvariant();

    public async Task<object> Languages(CancellationToken ct) => new
    {
        items = await db.CodeLists.AsNoTracking().Where(row => row.Kategorija == Settings.LanguagesCodebook &&
                (row.Definition == null || row.Definition.IsActive))
            .OrderBy(row => row.RedoslijedPrikaza).ThenBy(row => row.Kod)
            .Select(row => new { code = row.Kod, name = row.Naziv }).ToListAsync(ct)
    };

    private Task<bool> Supported(string language, CancellationToken ct) => db.CodeLists.AsNoTracking().AnyAsync(row =>
        row.Kategorija == Settings.LanguagesCodebook && row.Kod.ToUpper() == language && (row.Definition == null || row.Definition.IsActive), ct);

    public async Task<RuntimeResourcePack> Resources(string language, CancellationToken ct)
    {
        language = language.Trim().ToUpperInvariant();
        if (language.Length > 10 || !await Supported(language, ct))
            throw new BusinessRuleException(404, "localization.language_unavailable");
        var versions = await db.CodeListDefinitions.AsNoTracking().Where(row => row.Name == Settings.LanguagesCodebook || row.Name == Settings.LanguagePacksCodebook)
            .OrderBy(row => row.Name).Select(row => new { row.Id, row.Version, row.IsActive }).ToListAsync(ct);
        var version = string.Join(':', versions.Select(row => $"{row.Id:N}.{row.Version}.{row.IsActive}"));
        var cacheKey = $"pack:{Settings.LanguagesCodebook}:{Settings.LanguagePacksCodebook}:{language}:{DefaultLanguage}:{version}";
        if (cache.Values.TryGetValue(cacheKey, out RuntimeResourcePack? existing)) return existing!;
        var fallback = DefaultLanguage;
        var rows = await db.CodeLists.AsNoTracking().Where(row => row.Kategorija == Settings.LanguagePacksCodebook &&
                row.ResourceKey != null && row.LanguageCode != null && (row.LanguageCode == language || row.LanguageCode == fallback) &&
                (row.Definition == null || row.Definition.IsActive)).Take(10_001).ToListAsync(ct);
        if (rows.Count > 10_000) throw new BusinessRuleException(503, "localization.catalog_limit");
        var defaults = rows.Where(row => row.LanguageCode == fallback).ToDictionary(row => row.ResourceKey!, StringComparer.OrdinalIgnoreCase);
        var requested = rows.Where(row => row.LanguageCode == language).ToDictionary(row => row.ResourceKey!, StringComparer.OrdinalIgnoreCase);
        var keys = defaults.Keys.Concat(requested.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase);
        var items = keys.Select(key =>
        {
            defaults.TryGetValue(key, out var baseline); requested.TryGetValue(key, out var preferred);
            return new RuntimeResource(key, preferred?.ResourceLabel ?? baseline?.ResourceLabel ?? key,
                preferred?.ResourceHelper ?? baseline?.ResourceHelper, preferred?.ResourcePlaceholder ?? baseline?.ResourcePlaceholder);
        }).ToArray();
        var pack = new RuntimeResourcePack(language, fallback, version, items);
        cache.Values.Set(cacheKey, pack, new MemoryCacheEntryOptions { Size = Math.Max(1, items.Length), SlidingExpiration = TimeSpan.FromMinutes(10) });
        return pack;
    }

    public async Task<string> Text(string key, string? requestedLanguage, CancellationToken ct)
    {
        var language = (requestedLanguage?.Length <= 256 ? requestedLanguage : null)?.Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToUpperInvariant() ?? DefaultLanguage;
        if (!await Supported(language, ct)) language = DefaultLanguage;
        if (await Supported(language, ct))
        {
            var pack = await Resources(language, ct);
            var resource = pack.Items.FirstOrDefault(row => row.ResourceKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (resource is not null) return resource.Label;
        }
        var diagnostic = "missing:" + language + ":" + key;
        if (!cache.Values.TryGetValue(diagnostic, out _))
        {
            cache.Values.Set(diagnostic, true, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
            logger.LogWarning("Localization resource missing. Language {Language}. ResourceKey {ResourceKey}", language, key);
        }
        return key;
    }

    public async Task Put(string language, string key, PutRuntimeResource input, string actor, CancellationToken ct)
    {
        language = language.Trim().ToUpperInvariant();
        if (!await Supported(language, ct)) throw new BusinessRuleException(400, "localization.language_unavailable");
        if (string.IsNullOrWhiteSpace(input.Label) || input.Label.Length > 4000 || input.Helper?.Length > 4000 || input.Placeholder?.Length > 500 || key.Length > 200)
            throw new BusinessRuleException(400, "localization.invalid_resource");
        var normalized = key.ToUpperInvariant();
        var row = await db.CodeLists.IgnoreQueryFilters().SingleOrDefaultAsync(row => row.Kategorija == Settings.LanguagePacksCodebook &&
            row.LanguageCode == language && row.ResourceKey != null && row.ResourceKey.ToUpper() == normalized, ct);
        if (row is null && !Regex.IsMatch(key, "^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$", RegexOptions.CultureInvariant))
            throw new BusinessRuleException(400, "localization.invalid_key");
        if (row?.IsSystemRequired == true && !input.IsActive)
            throw new BusinessRuleException(409, "codelist.system_required");
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(item => item.Name == Settings.LanguagePacksCodebook && item.IsActive, ct)
            ?? throw new BusinessRuleException(503, "localization.setup_required");
        var now = DateTime.UtcNow;
        definition.ModifiedBy = actor;
        if (row is null)
        {
            row = new CodeList { Kategorija = definition.Name, DefinitionId = definition.Id, Kod = Guid.NewGuid().ToString("N"),
                Naziv = key, ResourceKey = key, LanguageCode = language, KreiranDatum = now, KreiraoKorisnik = actor };
            db.CodeLists.Add(row);
        }
        row.ResourceLabel = input.Label; row.ResourceHelper = input.Helper; row.ResourcePlaceholder = input.Placeholder;
        row.Aktivan = input.IsActive; row.IzmijenjenDatum = now; row.IzmijenioKorisnik = actor;
        db.AuditLogs.Add(new() { TableName = "CodeList", RecordId = row.Kod, Action = "LOCALIZATION_UPDATE",
            UserId = actor, Username = actor, Timestamp = now, NewValues = System.Text.Json.JsonSerializer.Serialize(new { language, key, input.IsActive }) });
        await db.SaveChangesAsync(ct);
    }
}
