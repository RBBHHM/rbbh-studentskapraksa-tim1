using System.Text.Json;
using System.Text.RegularExpressions;

namespace RBI.Localization.Internal;

internal static partial class RbiLocalizationResourceParser
{
    internal static IReadOnlyDictionary<string, string> Parse(string json, string expectedCulture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedCulture);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Localization resource '{expectedCulture}' is not valid JSON.", exception);
        }

        using (document)
        {
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
                throw new InvalidDataException($"Localization resource '{expectedCulture}' must be a JSON object.");

            EnsureNoDuplicateProperties(document.RootElement, expectedCulture, "root");

            if (!document.RootElement.TryGetProperty("culture", out var cultureElement)
                || cultureElement.ValueKind is not JsonValueKind.String)
            {
                throw new InvalidDataException($"Localization resource '{expectedCulture}' must declare a culture.");
            }

            var declaredCulture = cultureElement.GetString();
            if (!string.Equals(declaredCulture, expectedCulture, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Localization resource '{expectedCulture}' declares culture '{declaredCulture ?? "<null>"}'.");
            }

            if (!document.RootElement.TryGetProperty("translations", out var translations)
                || translations.ValueKind is not JsonValueKind.Object)
            {
                throw new InvalidDataException($"Localization resource '{expectedCulture}' must contain a translations object.");
            }

            EnsureNoDuplicateProperties(translations, expectedCulture, "translations");

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in translations.EnumerateObject())
            {
                if (!IsValidKey(property.Name))
                    throw new InvalidDataException($"Localization key '{property.Name}' in '{expectedCulture}' is invalid.");
                if (property.Value.ValueKind is not JsonValueKind.String)
                    throw new InvalidDataException($"Localization value '{property.Name}' in '{expectedCulture}' must be text.");

                result.Add(property.Name, property.Value.GetString() ?? string.Empty);
            }

            if (result.Count is 0)
                throw new InvalidDataException($"Localization resource '{expectedCulture}' contains no translations.");

            return result;
        }
    }

    internal static bool IsValidKey(string key) =>
        !string.IsNullOrWhiteSpace(key) && SemanticKeyPattern().IsMatch(key);

    private static void EnsureNoDuplicateProperties(JsonElement element, string culture, string section)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name))
                throw new InvalidDataException(
                    $"Localization resource '{culture}' contains duplicate key '{property.Name}' in {section}.");
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*(?:\\.[a-z][a-z0-9-]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex SemanticKeyPattern();
}
