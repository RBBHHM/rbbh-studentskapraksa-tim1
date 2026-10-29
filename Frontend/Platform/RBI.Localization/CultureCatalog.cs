using System.Globalization;
using RBI.Localization.Internal;

namespace RBI.Localization;

/// <summary>Startup snapshot of the application's validated static localization resources.</summary>
public sealed class RbiCultureCatalog
{
    /// <summary>Gets the discovered supported culture names, also used by requests and language selection.</summary>
    public IReadOnlyList<string> SupportedCultures { get; }
    /// <summary>Gets the configured fallback culture (BS by default).</summary>
    public string DefaultCulture { get; }
    internal IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Resources { get; }

    internal RbiCultureCatalog(string resourceDirectory, string defaultCulture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCulture);
        var known = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(culture => culture.Name.Length > 0)
            .Select(culture => culture.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!known.Contains(defaultCulture)) throw new InvalidDataException("Default localization culture is invalid.");
        DefaultCulture = CultureInfo.GetCultureInfo(defaultCulture).Name;
        if (!Directory.Exists(resourceDirectory)) throw new InvalidDataException("Localization resource directory was not found.");
        var resources = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(resourceDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!known.Contains(name)) throw new InvalidDataException($"Localization resource filename '{name}' is not a valid culture.");
            var culture = CultureInfo.GetCultureInfo(name).Name;
            var values = RbiLocalizationResourceParser.Parse(File.ReadAllText(file), culture);
            if (!resources.TryAdd(culture, values)) throw new InvalidDataException($"Duplicate localization culture '{culture}'.");
        }
        if (!resources.TryGetValue(DefaultCulture, out var fallback) || fallback.Count == 0)
            throw new InvalidDataException($"Default localization resource '{DefaultCulture}' is missing or empty.");
        var missing = resources.Values.SelectMany(values => values.Keys).Except(fallback.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length > 0) throw new InvalidDataException($"Default localization resource '{DefaultCulture}' is missing: {string.Join(", ", missing)}.");
        Resources = resources;
        // Preserve existing EN/DE/BS presentation order without treating it as a
        // support registry: every additional discovered culture is included.
        SupportedCultures = Array.AsReadOnly(resources.Keys.OrderBy(name => name switch { "en" => 0, "de" => 1, "bs" => 2, _ => 3 })
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}

/// <summary>Application localization configuration.</summary>
public sealed class RbiLocalizationOptions
{
    /// <summary>Gets or sets the fallback culture. Its resource must cover the application's complete key set.</summary>
    public string DefaultCulture { get; set; } = "bs";
}
