using System.Globalization;
using System.Text.Json;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components;

/// <summary>
/// Backend-owned language-pack text (notification, validation and workflow wording served by GET api/localization/resources/{language}).
/// Loaded per circuit and refreshed after a short time so administrative edits become visible without a restart; it never replaces the
/// frontend's static catalogs, it only answers keys those catalogs do not own.
/// </summary>
public sealed class RuntimeResources(ILocalizationApi api)
{
    private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(2), Retry = TimeSpan.FromSeconds(30);
    private Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private string _culture = string.Empty;
    private DateTime _loadedAt = DateTime.MinValue;
    private bool _failed;

    /// <summary>Loads (or refreshes) the pack for the current UI culture. Failures keep the previous values and retry later.</summary>
    public async Task EnsureAsync(CancellationToken token = default)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToUpperInvariant();
        var age = DateTime.UtcNow - _loadedAt;
        if (culture == _culture && age < (_failed ? Retry : Fresh)) return;
        try
        {
            var pack = await api.ResourcesAsync(culture, token);
            _values = pack.Items.ToDictionary(item => item.ResourceKey, item => item.Label, StringComparer.OrdinalIgnoreCase);
            _failed = false;
        }
        catch (RegistryApiException) { _failed = true; if (culture != _culture) _values = new(StringComparer.OrdinalIgnoreCase); }
        _culture = culture; _loadedAt = DateTime.UtcNow;
    }

    public bool TryGet(string key, out string text) => _values.TryGetValue(key, out text!);

    /// <summary>Replaces <c>{name}</c> placeholders with the notification's parameters; unknown placeholders stay visible rather than disappearing.</summary>
    public static string Fill(string template, JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object) return template;
        foreach (var property in parameters.EnumerateObject())
            template = template.Replace("{" + property.Name + "}", property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        return template;
    }
}
