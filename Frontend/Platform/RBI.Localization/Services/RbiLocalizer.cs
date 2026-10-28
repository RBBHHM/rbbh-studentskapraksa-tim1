using System.Globalization;
using Microsoft.Extensions.Logging;
using RBI.Localization.Internal;

namespace RBI.Localization;

internal sealed class RbiLocalizer(
    IRbiLocalizationProvider provider,
    RbiMissingKeyTracker missingKeys,
    ILogger<RbiLocalizer> logger,
    RbiCultureCatalog? catalog = null) : IRbiLocalizer
{
    public string this[string key] => Resolve(key);

    public string this[string key, params object?[] arguments] => ResolveFormatted(key, arguments);

    private string Resolve(string key)
    {
        ValidateKey(key);
        return Candidates(key).Select(candidate => candidate.Value).FirstOrDefault() ?? Missing(key);
    }

    private string ResolveFormatted(string key, object?[] arguments)
    {
        ValidateKey(key);
        foreach (var candidate in Candidates(key))
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, candidate.Value, arguments);
            }
            catch (FormatException)
            {
                logger.LogWarning(
                    "Localization value has an invalid composite format. Key={Key} Culture={Culture}.",
                    key,
                    candidate.Culture);
            }
        }

        return Missing(key);
    }

    private IEnumerable<(string Culture, string Value)> Candidates(string key)
    {
        foreach (var cultureName in BuildFallbackChain(CultureInfo.CurrentUICulture))
            foreach (var value in provider.GetValues(cultureName, key))
                yield return (cultureName, value);
    }

    private string Missing(string key)
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        if (missingKeys.MarkFirst(cultureName, key))
            logger.LogWarning("Missing localization key {Key} for culture {Culture}.", key, cultureName);

        return key;
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!RbiLocalizationResourceParser.IsValidKey(key))
            throw new ArgumentException("Localization keys must use lowercase semantic dot notation.", nameof(key));
    }

    private IEnumerable<string> BuildFallbackChain(CultureInfo requestedCulture)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var culture = requestedCulture;
             culture != CultureInfo.InvariantCulture && !string.IsNullOrWhiteSpace(culture.Name);
             culture = culture.Parent)
        {
            if (seen.Add(culture.Name)) yield return culture.Name;
        }

        var fallback = catalog?.DefaultCulture ?? RbiLocalizationDefaults.DefaultCulture;
        if (seen.Add(fallback)) yield return fallback;
    }
}
