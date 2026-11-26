using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class RuntimeLocalizationOptions
{
    public string LanguagesCodebook { get; set; } = LanguageBooks.Languages;
    public string LanguagePacksCodebook { get; set; } = LanguageBooks.Packs;
    public string DefaultLanguage { get; set; } = "BS";
}

public sealed record RuntimeResource(string ResourceKey, string Label, string? Helper, string? Placeholder, string? ContextInfo = null);
public sealed record RuntimeResourcePack(string Language, string FallbackLanguage, string Version, IReadOnlyList<RuntimeResource> Items);
public sealed record PutRuntimeResource(string Label, string? Helper = null, string? Placeholder = null, bool IsActive = true, string? ContextInfo = null);

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

    public async Task<object> Languages(CancellationToken ct)
    {
        var rows = await db.CodeLists.AsNoTracking().Where(row => row.Definition!.Name == Settings.LanguagesCodebook &&
                db.CodeListDefinitions.Any(definition => definition.Name == row.Definition!.Name && definition.IsActive))
            .OrderBy(row => row.Kod)
            .Select(row => new { row.Kod, row.Naziv, row.Localization!.LanguageCode, row.Localization!.ResourceKey }).ToListAsync(ct);
        return new { items = rows.Select(row => new { code = row.LanguageCode ?? row.Kod, name = row.ResourceKey ?? row.Naziv }) };
    }

    private Task<bool> Supported(string language, CancellationToken ct) => db.CodeLists.AsNoTracking().AnyAsync(row =>
        row.Definition!.Name == Settings.LanguagesCodebook && (row.Localization!.LanguageCode ?? row.Kod).ToUpper() == language && db.CodeListDefinitions.Any(definition => definition.Name == row.Definition!.Name && definition.IsActive), ct);

    public async Task<RuntimeResourcePack> Resources(string language, CancellationToken ct)
    {
        language = language.Trim().ToUpperInvariant();
        if (language.Length > 10 || !await Supported(language, ct))
            throw new BusinessRuleException(404, "localization.language_unavailable");
        var versions = await db.CodeListDefinitions.AsNoTracking().Where(row => row.Name == Settings.LanguagesCodebook || row.Name == Settings.LanguagePacksCodebook)
            .OrderBy(row => row.Name).Select(row => new { row.Id, row.ModifiedAt, row.IsActive }).ToListAsync(ct);
        var version = string.Join(':', versions.Select(row => $"{row.Id}.{row.ModifiedAt?.Ticks}.{row.IsActive}"));
        var cacheKey = $"pack:{Settings.LanguagesCodebook}:{Settings.LanguagePacksCodebook}:{language}:{DefaultLanguage}:{version}";
        if (cache.Values.TryGetValue(cacheKey, out RuntimeResourcePack? existing)) return existing!;
        var fallback = DefaultLanguage;
        var rows = await db.CodeLists.AsNoTracking().Where(row => row.Definition!.Name == Settings.LanguagePacksCodebook &&
                row.Localization != null && row.Localization.ResourceKey != null && row.Localization.LanguageCode != null && (row.Localization!.LanguageCode == language || row.Localization!.LanguageCode == fallback) &&
                db.CodeListDefinitions.Any(definition => definition.Name == row.Definition!.Name && definition.IsActive)).Take(10_001).ToListAsync(ct);
        if (rows.Count > 10_000) throw new BusinessRuleException(503, "localization.catalog_limit");
        var defaults = rows.Where(row => row.Localization!.LanguageCode == fallback).ToDictionary(row => row.Localization!.ResourceKey!, StringComparer.OrdinalIgnoreCase);
        var requested = rows.Where(row => row.Localization!.LanguageCode == language).ToDictionary(row => row.Localization!.ResourceKey!, StringComparer.OrdinalIgnoreCase);
        var keys = defaults.Keys.Concat(requested.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase);
        var items = keys.Select(key =>
        {
            defaults.TryGetValue(key, out var baseline); requested.TryGetValue(key, out var preferred);
            return new RuntimeResource(key, preferred?.Localization?.ResourceLabel ?? baseline?.Localization?.ResourceLabel ?? key,
                preferred?.Localization?.ResourceHelper ?? baseline?.Localization?.ResourceHelper, preferred?.Localization?.ResourcePlaceholder ?? baseline?.Localization?.ResourcePlaceholder,
                preferred?.Localization?.ContextInfo ?? baseline?.Localization?.ContextInfo);
        }).ToArray();
        var pack = new RuntimeResourcePack(language, fallback, version, items);
        cache.Values.Set(cacheKey, pack, new MemoryCacheEntryOptions { Size = Math.Max(1, items.Length), AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) });
        return pack;
    }

    public async Task<RuntimeResourcePack?> ForRequest(string? header, CancellationToken ct)
    {
        var language = (header?.Length <= 256 ? header : null)?.Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToUpperInvariant() ?? DefaultLanguage;
        if (string.IsNullOrEmpty(language)) language = DefaultLanguage;
        try { return await Resources(language, ct); }
        catch (BusinessRuleException error) when (error.Code == "localization.language_unavailable")
        {
            if (language == DefaultLanguage) return null;
            try { return await Resources(DefaultLanguage, ct); }
            catch (BusinessRuleException fallback) when (fallback.Code == "localization.language_unavailable") { return null; }
        }
    }

    public async Task<string> Text(string key, string? requestedLanguage, CancellationToken ct)
    {
        var pack = await ForRequest(requestedLanguage, ct);
        var resource = pack?.Items.FirstOrDefault(row => row.ResourceKey.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (resource is not null) return resource.Label;
        var language = pack?.Language ?? DefaultLanguage;
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
        if (string.IsNullOrWhiteSpace(input.Label) || input.Label.Length > 4000 || input.Helper?.Length > 4000 || input.Placeholder?.Length > 4000 || input.ContextInfo?.Length > 100 || key.Length > 200)
            throw new BusinessRuleException(400, "localization.invalid_resource");
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(item => item.Name == Settings.LanguagePacksCodebook && item.IsActive, ct)
            ?? throw new BusinessRuleException(503, "localization.setup_required");
        var normalized = key.ToUpperInvariant();
        var row = await db.CodeLists.IgnoreQueryFilters().SingleOrDefaultAsync(row => row.Definition!.Name == Settings.LanguagePacksCodebook &&
            row.Localization != null && row.Localization.LanguageCode == language && row.Localization.ResourceKey != null && row.Localization.ResourceKey.ToUpper() == normalized, ct);
        if (row is null && !Regex.IsMatch(key, "^[a-z][a-z0-9]*(?:[.-][a-z][a-z0-9]*)+$", RegexOptions.CultureInvariant))
            throw new BusinessRuleException(400, "localization.invalid_key");
        if (row is not null && definition.IsSystemRequired && !input.IsActive)
            throw new BusinessRuleException(409, "codelist.system_required");
        var before = row is null ? null : Snapshot(row);
        var now = DateTime.UtcNow;
        definition.ModifiedBy = actor;
        definition.ModifiedAt = now;
        if (row is null)
        {
            row = new CodeList { Kategorija = definition.Name, Kod = Guid.NewGuid().ToString("N"),
                Naziv = key, Localization = new() { ResourceKey = key, LanguageCode = language }, KreiranDatum = now, KreiraoKorisnik = actor };
            db.CodeLists.Add(row);
        }
        row.Localization!.ResourceLabel = input.Label;
        row.Localization.ResourceHelper = input.Helper;
        row.Localization.ResourcePlaceholder = input.Placeholder;
        if (input.ContextInfo is not null) row.Localization.ContextInfo = string.IsNullOrWhiteSpace(input.ContextInfo) ? null : input.ContextInfo.Trim();
        row.Aktivan = input.IsActive; row.IzmijenjenDatum = now; row.IzmijenioKorisnik = actor;
        db.AuditLogs.Add(BusinessAudit.Create("CodeList", "LOCALIZATION_UPDATE",
            actor, null, before, Snapshot(row)));
        await db.SaveChangesAsync(ct);
    }

    private static object Snapshot(CodeList row) => new
    {
        Language = row.Localization!.LanguageCode,
        Key = row.Localization.ResourceKey,
        Label = row.Localization.ResourceLabel,
        Helper = row.Localization.ResourceHelper,
        Placeholder = row.Localization.ResourcePlaceholder,
        row.Localization.ContextInfo,
        IsActive = row.Aktivan
    };
}
