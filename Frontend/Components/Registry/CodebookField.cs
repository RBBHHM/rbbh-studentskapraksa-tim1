using System.Globalization;
using System.Text.Json;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

internal sealed class CodebookField(CodebookColumn column, JsonElement existing)
{
    public CodebookColumn Column { get; } = column;
    public string? Text { get; set; } = existing.ValueKind switch
    {
        JsonValueKind.String => existing.GetString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => existing.GetRawText(),
        _ => null
    };

    public DateTime? Date
    {
        get => DateTime.TryParse(Text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? date : null;
        set => Text = value?.ToString(Column.Slot == 11 ? "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF" : "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string TypeKey(int slot) => slot switch
    {
        1 or 2 => "codes.csv.type-boolean",
        >= 3 and <= 5 => "codes.csv.type-integer",
        >= 6 and <= 8 => "codes.csv.type-decimal",
        9 or 10 => "codes.csv.type-date",
        11 => "codes.csv.type-datetime",
        _ => "codes.csv.type-text"
    };

    public static int TextLimit(int slot) => slot switch { 12 => 10, 13 => 200, 14 or 15 => 100, _ => 4000 };
}
