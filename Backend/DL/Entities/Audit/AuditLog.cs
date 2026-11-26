using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RBBH.ConnectedParties.DL.Entities.Audit;

/// <summary>
/// Audit log — bilježi sve promjene u sistemu.
/// </summary>
public class AuditLog
{
    [Key]
    public long Id { get; set; }

    [Required][StringLength(100)]
    public string TableName { get; set; } = string.Empty;

    private const string ReferenceKey = "_recordId";
    private string? _recordId;

    [NotMapped]
    public string RecordId
    {
        get => _recordId ?? ReadReference(NewValues) ?? ReadReference(OldValues) ?? string.Empty;
        set => _recordId = value;
    }

    internal void EmbedReference()
    {
        if (string.IsNullOrEmpty(_recordId)) return;
        if (NewValues is null && OldValues is not null)
            OldValues = WithReference(OldValues, _recordId);
        else
            NewValues = WithReference(NewValues, _recordId);
    }

    private static string WithReference(string? payload, string reference)
    {
        JsonNode? value = null;
        if (payload is not null)
        {
            try { value = JsonNode.Parse(payload); }
            catch (JsonException) { value = JsonValue.Create(payload); }
        }
        var result = value as JsonObject ?? new JsonObject { ["value"] = value };
        result[ReferenceKey] = reference;
        return result.ToJsonString();
    }

    internal static string? SnapshotWithoutReference(string? payload)
    {
        if (payload is null) return null;
        try
        {
            if (JsonNode.Parse(payload) is not JsonObject snapshot || !snapshot.Remove(ReferenceKey))
                return payload;
            return snapshot.Count == 0 || (snapshot.Count == 1 && snapshot.ContainsKey("value") && snapshot["value"] is null)
                ? null
                : snapshot.ToJsonString();
        }
        catch (JsonException) { return payload; }
    }

    private static string? ReadReference(string? payload)
    {
        if (payload is null) return null;
        try { return (JsonNode.Parse(payload) as JsonObject)?[ReferenceKey]?.GetValue<string>(); }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return null; }
    }


    /// <summary>CREATE, UPDATE, DEACTIVATE, REACTIVATE, ROLE_ASSIGN, ROLE_REMOVE,
    /// ADMIN_TRANSFER, CODELIST_ADD, CODELIST_UPDATE, CODELIST_DELETE</summary>
    [Required][StringLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    [Required][StringLength(100)]
    public string Username { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
