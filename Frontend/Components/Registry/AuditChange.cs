using System.Text.Json;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public enum AuditChangeKind { Added, Removed, Changed, Unchanged }

public sealed record AuditFieldChange(string Field, string? Old, string? New, AuditChangeKind Kind);

/// <summary>Field-level comparison of the audit old/new JSON snapshots; display only.</summary>
public static class AuditChange
{
    public static IReadOnlyList<AuditFieldChange> Compare(string? oldValues, string? newValues)
    {
        var before = Flatten(oldValues);
        var after = Flatten(newValues);
        if (before is null || after is null) return [];
        return before.Keys.Union(after.Keys).Order(StringComparer.OrdinalIgnoreCase).Select(field =>
        {
            before.TryGetValue(field, out var oldValue);
            after.TryGetValue(field, out var newValue);
            var kind = oldValue is null && newValue is not null ? AuditChangeKind.Added
                : oldValue is not null && newValue is null ? AuditChangeKind.Removed
                : oldValue == newValue ? AuditChangeKind.Unchanged : AuditChangeKind.Changed;
            return new AuditFieldChange(field, oldValue, newValue, kind);
        }).ToList();
    }

    /// <summary>Visual weight of an action code: create green, edit yellow, remove red.</summary>
    public static string Tone(string? action) => action?.ToUpperInvariant() switch
    {
        "CREATE" or "INSERT" or "ADD" or "REACTIVATE" => "success",
        "DELETE" or "REMOVE" or "DEACTIVATE" or "REJECT" => "danger",
        "UPDATE" or "EDIT" or "MODIFY" or "VERIFY" => "warning",
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
            foreach (var property in document.RootElement.EnumerateObject())
                result[property.Name] = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.ToString();
        }
        catch (JsonException) { return null; }
        return result;
    }
}
