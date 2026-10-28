using Microsoft.Extensions.Logging;

namespace RBI.Localization.Internal;

internal sealed class RbiStaticJsonLocalizationProvider : IRbiLocalizationProvider
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _resources;

    public RbiStaticJsonLocalizationProvider(
        RbiCultureCatalog catalog,
        ILogger<RbiStaticJsonLocalizationProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(logger);
        _resources = catalog.Resources;
        LogTranslationGaps(_resources, logger);
    }

    internal RbiStaticJsonLocalizationProvider(
        string resourceDirectory,
        ILogger<RbiStaticJsonLocalizationProvider> logger) : this(new RbiCultureCatalog(resourceDirectory, RbiLocalizationDefaults.DefaultCulture), logger)
    {
    }

    public IEnumerable<string> GetValues(string cultureName, string key)
    {
        if (_resources.TryGetValue(cultureName, out var translations)
            && translations.TryGetValue(key, out var found))
            yield return found;
    }

    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> FindMissingTranslations(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> resources)
    {
        var allKeys = resources.Values.SelectMany(values => values.Keys).ToHashSet(StringComparer.Ordinal);
        return resources.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)allKeys.Except(pair.Value.Keys, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void LogTranslationGaps(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> resources,
        ILogger logger)
    {
        foreach (var (culture, keys) in FindMissingTranslations(resources))
        {
            if (keys.Count > 0)
                logger.LogWarning("Localization resource {Culture} is missing keys: {Keys}", culture, string.Join(", ", keys));
        }
    }
}
