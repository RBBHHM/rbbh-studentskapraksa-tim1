using System.Text.Json;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public enum AuditChangeKind { Added, Removed, Changed, Unchanged }

public sealed record AuditFieldChange(string Field, string? Old, string? New, AuditChangeKind Kind);

/// <summary>Field-level comparison of the audit old/new JSON snapshots; display only.</summary>
public static class AuditChange
{
    public static IReadOnlyList<AuditFieldChange> Compare(string? oldValues, string? newValues,
        IReadOnlyDictionary<long, string>? permissionCodes = null, string yes = "Yes", string no = "No",
        string unavailable = "Unavailable reference",
        IReadOnlyDictionary<string, Dictionary<long, string>>? referenceNames = null)
    {
        var before = Flatten(oldValues);
        var after = Flatten(newValues);
        if (before is null || after is null) return [];
        return before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Select(field =>
        {
            before.TryGetValue(field, out var oldValue);
            after.TryGetValue(field, out var newValue);
            var kind = oldValue is null && newValue is not null ? AuditChangeKind.Added
                : oldValue is not null && newValue is null ? AuditChangeKind.Removed
                : oldValue == newValue ? AuditChangeKind.Unchanged : AuditChangeKind.Changed;
            return new AuditFieldChange(field, Display(field, oldValue), Display(field, newValue), kind);
        }).ToList();

        string? Display(string field, string? value)
        {
            if (value is null) return null;
            using var document = JsonDocument.Parse(value);
            string Format(JsonElement element) => element.ValueKind switch
            {
                JsonValueKind.True => yes,
                JsonValueKind.False => no,
                JsonValueKind.Null => "—",
                _ => element.ToString()
            };

            var element = document.RootElement;
            if (element.ValueKind != JsonValueKind.Array)
            {
                var name = field[(field.LastIndexOf('.') + 1)..];
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var referenceId) &&
                    referenceNames?.FirstOrDefault(pair => pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value is { } names)
                    return names.GetValueOrDefault(referenceId) ?? unavailable;
                return Format(element);
            }
            if (IsPermissionIds(field))
                return string.Join(", ", element.EnumerateArray().Select(item =>
                    item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var id)
                        ? permissionCodes?.GetValueOrDefault(id) ?? unavailable
                        : Format(item)));
            return string.Join(", ", element.EnumerateArray().Select(Format));
        }
    }

    private static bool IsPermissionIds(string field) =>
        field.Equals("PermissionIds", StringComparison.OrdinalIgnoreCase) ||
        field.EndsWith(".PermissionIds", StringComparison.OrdinalIgnoreCase);

    /// <summary>Visual weight of an action code: create green, edit yellow, remove red.</summary>
    public static string Tone(string? action) => action?.ToUpperInvariant() switch
    {
        "CREATE" or "INSERT" or "ADD" or "REACTIVATE" or "ROLE_CATALOG_CREATE" or "CODELIST_ADD" or "ROLE_ASSIGN" or "WORK_AUTH_INSERT" => "success",
        "DELETE" or "ROLE_CATALOG_DELETE" or "REMOVE" or "DEACTIVATE" or "REJECT" or "CODELIST_DELETE" or "ROLE_REMOVE" or "WORK_AUTH_DELETE" => "danger",
        "UPDATE" or "EDIT" or "MODIFY" or "VERIFY" or "ROLE_CATALOG_UPDATE" or "CODELIST_UPDATE" or "LOCALIZATION_UPDATE" or "WORK_AUTH_UPDATE" => "warning",
        _ => "neutral"
    };

    private static Dictionary<string, string?>? Flatten(string? json)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            AddFields(document.RootElement, "", result);
        }
        catch (JsonException) { return null; }
        return result;
    }

    private static void AddFields(JsonElement element, string path, Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    AddFields(property.Value, path.Length == 0 ? property.Name : $"{path}.{property.Name}", result);
                break;
            case JsonValueKind.Array:
                var items = element.EnumerateArray().ToArray();
                if (IsPermissionIds(path))
                {
                    result[path] = "[" + string.Join(",", items.Select(item => item.GetRawText()).Order(StringComparer.Ordinal)) + "]";
                }
                else if (items.Any(item => item.ValueKind is JsonValueKind.Object or JsonValueKind.Array))
                {
                    for (var index = 0; index < items.Length; index++)
                        AddFields(items[index], $"{path} [{index + 1}]", result);
                }
                else if (items.Length > 0)
                {
                    result[path] = JsonSerializer.Serialize(element);
                }
                break;
            default:
                result[path] = element.ValueKind == JsonValueKind.Null ? null : element.GetRawText();
                break;
        }
    }
}
