using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.BL.Services;

public sealed record CodebookColumn(int Slot, string Name, string? Description, int? Position)
{
    public string TypeKey => Slot switch
    {
        1 or 2 => "codes.csv.type-boolean",
        >= 3 and <= 5 => "codes.csv.type-integer",
        >= 6 and <= 8 => "codes.csv.type-decimal",
        9 or 10 => "codes.csv.type-date",
        11 => "codes.csv.type-datetime",
        _ => "codes.csv.type-text"
    };
}
public sealed record CsvIssue(string Key, params string[] Arguments);
public sealed record CsvPreview(int TotalRows, int Ready, int Duplicates, int Imported, bool ExampleSkipped, IReadOnlyList<CsvIssue> Errors,
    bool AllowsPartial = false);

internal sealed class CodebookCsv(ConnectedPartiesDbContext db)
{
    private const int RowStatus = -1;
    internal const int MaximumBytes = 10 * 1024 * 1024;
    private const int MaximumRows = 5000;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly Regex Code = new("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex ResourceKey = new("^[a-z][a-z0-9]*(?:[.-][a-z][a-z0-9]*)+$", RegexOptions.CultureInvariant);
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "M/d/yyyy", "d.M.yyyy"];
    private static readonly string[] DateTimeFormats = ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"];

    internal static IReadOnlyList<CodebookColumn> Columns(ConnectedPartiesDbContext db, CodeListDefinition definition)
    {
        var entry = db.Entry(definition);
        return Enumerable.Range(1, 20).Select(slot => new CodebookColumn(slot,
            entry.Property<string>($"ColumnName{slot}").CurrentValue?.Trim() ?? "",
            entry.Property<string>($"ColumnDescription{slot}").CurrentValue,
            entry.Property<int?>($"ColumnPosition{slot}").CurrentValue))
            .Where(column => column.Name.Length > 0).OrderBy(column => column.Position ?? column.Slot)
            .ThenBy(column => column.Slot).ToArray();
    }

    internal static object? ReadValue(ConnectedPartiesDbContext db, CodeList row, int slot) => slot switch
    {
        15 => string.IsNullOrEmpty(row.Kod) ? null : row.Kod,
        17 => string.IsNullOrEmpty(row.Naziv) ? null : row.Naziv,
        18 => row.Opis,
        12 => row.Localization?.LanguageCode,
        13 => row.Localization?.ResourceKey,
        14 => row.Localization?.ContextInfo,
        16 => row.Localization?.ResourceLabel,
        19 => row.Localization?.ResourceHelper,
        20 => row.Localization?.ResourcePlaceholder,
        _ => db.Entry(row).Property($"Slot{slot}").CurrentValue
    };

    internal async Task<CodeListDefinition?> Find(string category, CancellationToken ct) =>
        await db.CodeListDefinitions.SingleOrDefaultAsync(row =>
            row.Name.ToUpper() == category.ToUpper() && row.IsActive, ct);

    internal sealed record Field(string Header, int Slot, bool Required, string Example, int? MaxLength = null, bool IsCode = false);
    internal static Field Slot(int slot, string header, bool required = false) => new(header, slot, required,
        slot switch
        {
            RowStatus or 1 or 2 => "true", >= 3 and <= 5 => "42", >= 6 and <= 8 => "123.45",
            9 or 10 => "2026-12-31", 11 => "2026-12-31T13:45:00", _ => "Example"
        });

    private Field[] Schema(CodeListDefinition definition, string exampleLanguage)
    {
        var columns = Columns(db, definition);
        if (!CodebookDefinitions.UsesReferenceFields(definition)) return columns.Select(column => Slot(column.Slot, column.Name)).ToArray();
        if (definition.Name == LanguageBooks.Packs)
            return [Slot(RowStatus, "IS_ACTIVE"), new("LANGUAGE_CODE", 12, true, exampleLanguage), new("RESOURCE_KEY", 13, true, "example.field"),
                Slot(14, "CONTEXT_INFO"), Slot(16, "LABEL_TEXT", true), Slot(19, "HELP_TEXT"), Slot(20, "PLACEHOLDER_TEXT"),
                .. columns.Where(column => column.Slot is not (1 or 12 or 13 or 14 or 16 or 19 or 20)).Select(column => Slot(column.Slot, column.Name))];
        if (definition.Name == LanguageBooks.Languages)
            return [Slot(RowStatus, "IS_ACTIVE"), new("LANGUAGE_CODE", 12, true, "XX"), new("LANGUAGE_NAME", 13, true, "Example language"),
                .. columns.Where(column => column.Slot is not (1 or 12 or 13)).Select(column => Slot(column.Slot, column.Name))];
        return [new("VALUE_KEY", 15, true, "EXAMPLE_CODE", 50, IsCode: true), new("NAME", 17, true, "Example value", 200),
            new("DESCRIPTION", 18, false, "Example description", 500), Slot(RowStatus, columns.FirstOrDefault(column => column.Slot == 1)?.Name ?? "IS_ACTIVE"),
            .. columns.Where(column => column.Slot is not (1 or 15 or 17 or 18)).Select(column => Slot(column.Slot, column.Name))];
    }

    internal async Task<byte[]?> Template(CodeListDefinition definition, CancellationToken ct)
    {
        var schema = Schema(definition, definition.Name == LanguageBooks.Packs ? await ExampleLanguage(ct) : "BS");
        if (!ValidSchema(schema)) return null;
        var csv = string.Join(',', schema.Select(field => Escape(field.Header))) + "\r\n" +
                  string.Join(',', schema.Select(field => Escape(field.Example))) + "\r\n";
        return [.. Encoding.UTF8.GetPreamble(), .. Utf8.GetBytes(csv)];
    }

    private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    private static bool ValidSchema(Field[] schema) =>
        schema.Length > 0 && schema.All(field => field.Header[0] is not ('=' or '+' or '-' or '@') && !field.Header.Any(char.IsControl)) &&
        schema.Select(field => field.Header).Distinct(StringComparer.OrdinalIgnoreCase).Count() == schema.Length;

    private async Task<string> ExampleLanguage(CancellationToken ct)
    {
        var codes = await Languages(ct);
        return codes.Contains("BS") ? "BS" : codes.Order(StringComparer.Ordinal).FirstOrDefault() ?? "BS";
    }

    private async Task<HashSet<string>> Languages(CancellationToken ct)
    {
        var values = await db.CodeLists.Where(row => row.Definition!.Name == LanguageBooks.Languages &&
            db.CodeListDefinitions.Any(definition => definition.Name == LanguageBooks.Languages && definition.IsActive))
            .Select(row => row.Localization!.LanguageCode ?? row.Kod).ToListAsync(ct);
        return values.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal async Task<CsvPreview> Import(CodeListDefinition definition, Stream file, bool dryRun, string actor, CancellationToken ct, bool allowPartial = false)
    {
        var packs = definition.Name == LanguageBooks.Packs;
        var languages = definition.Name == LanguageBooks.Languages;
        var supported = packs ? await Languages(ct) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var schema = Schema(definition, supported.Contains("BS") ? "BS" : supported.Order(StringComparer.Ordinal).FirstOrDefault() ?? "BS");
        if (!ValidSchema(schema)) return Failure("codes.csv.schema");
        string content;
        try
        {
            using var reader = new StreamReader(file, Utf8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            content = (await reader.ReadToEndAsync(ct)).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException) { return Failure("codes.csv.encoding"); }
        if (string.IsNullOrWhiteSpace(content)) return Failure("codes.csv.file");
        if (content.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
            return Failure("codes.csv.encoding");

        var delimiter = Delimiter(content.TrimStart(' ', '\r', '\n'));
        if (InvalidQuoteLine(content, delimiter[0]) is { } invalidLine)
            return Failure("codes.csv.syntax", invalidLine.ToString(Invariant));
        using var parser = new TextFieldParser(new StringReader(content)) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(delimiter);
        string[] header;
        try { header = parser.ReadFields()?.Select(value => value.Trim()).ToArray() ?? []; }
        catch (MalformedLineException) { return Failure("codes.csv.syntax", "1"); }
        var repeated = header.GroupBy(value => value, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (repeated is not null) return Failure("codes.csv.header-duplicate", repeated.Key[..Math.Min(repeated.Key.Length, 128)]);
        var byHeader = header.Select((value, index) => (value, index)).ToDictionary(entry => entry.value, entry => entry.index, StringComparer.OrdinalIgnoreCase);
        var unexpected = header.FirstOrDefault(value => !schema.Any(field => field.Header.Equals(value, StringComparison.OrdinalIgnoreCase)));
        var missing = schema.FirstOrDefault(field => !byHeader.ContainsKey(field.Header));
        if (unexpected is not null) return Failure("codes.csv.header-extra", unexpected.Length == 0 ? "—" : unexpected[..Math.Min(unexpected.Length, 128)]);
        if (missing is not null) return Failure("codes.csv.header-missing", missing.Header);

        var indices = schema.Select(field => byHeader[field.Header]).ToArray();
        var rows = db.CodeLists.IgnoreQueryFilters().Where(row => row.Definition!.Name == definition.Name);
        HashSet<string> used;
        if (!CodebookDefinitions.UsesReferenceFields(definition))
        {
            var existing = await rows.ToListAsync(ct);
            used = existing.Select(row => CellKey(schema.Select(field => ReadValue(db, row, field.Slot))))
                .ToHashSet(StringComparer.Ordinal);
        }
        else
        {
            var existing = await rows.Select(row => new { row.Kod, row.Localization!.LanguageCode, row.Localization!.ResourceKey }).ToListAsync(ct);
            used = existing.Select(row => packs ? Pair(row.LanguageCode, row.ResourceKey) :
                languages ? row.LanguageCode ?? row.Kod : row.Kod).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        var seen = new HashSet<string>(used.Comparer);
        var pending = new List<Dictionary<int, object?>>();
        var issues = new List<CsvIssue>();
        var duplicates = 0;
        var total = 0;
        var exampleSkipped = false;
        while (!parser.EndOfData)
        {
            ct.ThrowIfCancellationRequested();
            var line = parser.LineNumber.ToString(Invariant);
            if (++total > MaximumRows) return Failure("codes.csv.limit");
            string[] cells;
            try { cells = parser.ReadFields() ?? []; }
            catch (MalformedLineException) { return Failure("codes.csv.syntax", line); }
            if (cells.Length != header.Length) { issues.Add(new("codes.csv.cells", line)); continue; }
            if (cells.All(string.IsNullOrWhiteSpace)) { total--; continue; }
            if (total == 1 && indices.Select(i => cells[i]).SequenceEqual(schema.Select(field => field.Example), StringComparer.Ordinal))
            {
                exampleSkipped = true;
                continue;
            }

            var values = new Dictionary<int, object?>();
            for (var i = 0; i < schema.Length; i++)
            {
                var field = schema[i];
                var input = cells[indices[i]];
                if (!TryValue(field, input, out var value))
                {
                    issues.Add(new(string.IsNullOrWhiteSpace(input) ? "codes.csv.required" : "codes.csv.invalid", line, field.Header, field.Example));
                    continue;
                }
                values.Add(field.Slot, value);
            }
            if (values.Count != schema.Length) continue;
            if (!CodebookDefinitions.UsesReferenceFields(definition))
            {
                var signature = CellKey(schema.Select(field => values[field.Slot]));
                if (!seen.Add(signature))
                {
                    issues.Add(new("codes.csv.row-duplicate", line));
                    continue;
                }
                if (used.Contains(signature))
                {
                    duplicates++;
                    continue;
                }
                pending.Add(values);
                continue;
            }
            var code = (string)values[packs || languages ? 12 : 15]!;
            string? resourceKey = packs ? (string?)values[13] : null;
            if (packs && !supported.Contains(code))
            {
                issues.Add(new("codes.csv.language", line, code));
                continue;
            }
            if (packs && !ResourceKey.IsMatch(resourceKey!))
            {
                issues.Add(new("codes.csv.resource", line));
                continue;
            }
            if (languages && !Regex.IsMatch(code, "^[A-Za-z]{2,3}$", RegexOptions.CultureInvariant))
            {
                issues.Add(new("codes.csv.invalid", line, "LANGUAGE_CODE", "BS"));
                continue;
            }

            var key = packs ? Pair(code, resourceKey) : code;
            if (!seen.Add(key)) { issues.Add(new("codes.csv.file-duplicate", line, key.Replace("\u001f", " / "))); continue; }
            if (used.Contains(key)) { duplicates++; continue; }
            pending.Add(values);
        }

        if (total == 0) return Failure("codes.csv.data");
        var imported = 0;
        if (!dryRun && pending.Count > 0 && (issues.Count == 0 || (allowPartial && !CodebookDefinitions.UsesReferenceFields(definition))))
        {
            var now = DateTime.UtcNow;
            foreach (var values in pending) Add(definition, values, actor, now);
            definition.ModifiedBy = actor;
            db.AuditLogs.Add(BusinessAudit.Create("CodeList", "IMPORT", actor, null,
                newValues: new { Imported = pending.Count, Duplicates = duplicates }));
            await db.SaveChangesAsync(ct);
            imported = pending.Count;
        }
        return new(total - (exampleSkipped ? 1 : 0), pending.Count, duplicates, imported, exampleSkipped, issues, !CodebookDefinitions.UsesReferenceFields(definition));
    }

    private static CsvPreview Failure(string key, params string[] parameters) => new(0, 0, 0, 0, false, [new(key, parameters)]);
    private static string CellKey(IEnumerable<object?> values) => System.Text.Json.JsonSerializer.Serialize(values.Select(value => value switch
    {
        decimal number => number.ToString("G29", Invariant),
        IFormattable formattable => formattable.ToString(null, Invariant),
        _ => value?.ToString()
    }));

    private static string Pair(string? language, string? key) => language + "\u001f" + key;

    private static string Delimiter(string text)
    {
        var quoted = false;
        foreach (var character in text)
        {
            if (character == '"') quoted = !quoted;
            else if (!quoted && character is ',' or ';' or '\t') return character.ToString();
            else if (!quoted && character is '\r' or '\n') break;
        }
        return ",";
    }

    private static int? InvalidQuoteLine(string text, char delimiter)
    {
        var line = 1;
        var openingLine = 0;
        var atStart = true;
        var closed = false;
        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (character is '\r' or '\n')
            {
                if (character == '\r' || i == 0 || text[i - 1] != '\r') line++;
                if (openingLine == 0) { atStart = true; closed = false; }
                continue;
            }
            if (openingLine != 0)
            {
                if (character != '"') continue;
                if (i + 1 < text.Length && text[i + 1] == '"') { i++; continue; }
                openingLine = 0;
                closed = true;
                continue;
            }
            if (character == delimiter) { atStart = true; closed = false; continue; }
            if (character == '"')
            {
                if (!atStart || closed) return line;
                openingLine = line;
                atStart = false;
            }
            else if (!char.IsWhiteSpace(character))
            {
                if (closed) return line;
                atStart = false;
            }
        }
        return openingLine == 0 ? null : openingLine;
    }

    internal static bool TryValue(Field field, string input, out object? value)
    {
        value = null;
        var trimmed = input.Trim();
        if (trimmed.Length == 0) return !field.Required;
        switch (field.Slot)
        {
            case RowStatus or 1 or 2:
                if (trimmed is "0" or "1") { value = trimmed == "1"; return true; }
                if (!bool.TryParse(trimmed, out var flag)) return false;
                value = flag;
                return true;
            case >= 3 and <= 5:
                if (!int.TryParse(trimmed, NumberStyles.AllowLeadingSign, Invariant, out var integer)) return false;
                value = integer;
                return true;
            case >= 6 and <= 8:
                var scale = field.Slot == 8 ? 6 : 2;
                if (!trimmed.Contains('.')) trimmed = trimmed.Replace(',', '.');
                var wholeDigits = field.Slot == 8 ? 13 : 16;
                if (!Regex.IsMatch(trimmed, $@"^[+-]?[0-9]{{1,{wholeDigits}}}(\.[0-9]{{1,{scale}}})?$", RegexOptions.CultureInvariant) ||
                    !decimal.TryParse(trimmed, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, Invariant, out var number)) return false;
                value = number;
                return true;
            case 9 or 10 or 11:
                var formats = field.Slot == 11 ? DateTimeFormats : DateFormats;
                if (!DateTime.TryParseExact(trimmed, formats, Invariant, DateTimeStyles.None, out var date)) return false;
                value = date;
                return true;
            default:
                var limit = field.MaxLength ?? (field.Slot switch { 13 => 200, 12 => 10, 14 or 15 => 100, _ => 4000 });
                if (trimmed.Length > limit || (field.IsCode && !Code.IsMatch(trimmed))) return false;
                value = field.IsCode ? trimmed.ToUpperInvariant() : trimmed;
                return true;
        }
    }

    private void Add(CodeListDefinition definition, Dictionary<int, object?> values, string actor, DateTime now)
    {
        string? Text(int slot) => (string?)values.GetValueOrDefault(slot);
        var packs = definition.Name == LanguageBooks.Packs;
        var languages = definition.Name == LanguageBooks.Languages;
        var code = !CodebookDefinitions.UsesReferenceFields(definition) ? "" : packs ? Guid.NewGuid().ToString("N") :
            languages ? Text(12)!.ToUpperInvariant() : Text(15)!;
        var row = new CodeList
        {
            Definition = definition, Kategorija = definition.Name, Kod = code,
            Naziv = !CodebookDefinitions.UsesReferenceFields(definition) ? "" : packs || languages ? Text(13)! : Text(17)!,
            Opis = Text(18), Aktivan = (bool?)values.GetValueOrDefault(RowStatus) ?? true,
            KreiraoKorisnik = actor, KreiranDatum = now
        };
        db.CodeLists.Add(row);
        WriteSlots(db, row, values, packs || languages);
    }

    internal static void WriteSlots(ConnectedPartiesDbContext db, CodeList row, Dictionary<int, object?> values, bool languageBook = false)
    {
        string? Text(int slot) => (string?)values.GetValueOrDefault(slot);
        if (values.Keys.Any(slot => slot is 12 or 13 or 14 or 16 or 19 or 20))
        {
            row.Localization ??= new();
            if (values.ContainsKey(12)) row.Localization.LanguageCode = languageBook ? Text(12)?.ToUpperInvariant() : Text(12);
            if (values.ContainsKey(13)) row.Localization.ResourceKey = Text(13);
            if (values.ContainsKey(14)) row.Localization.ContextInfo = Text(14);
            if (values.ContainsKey(16)) row.Localization.ResourceLabel = Text(16);
            if (values.ContainsKey(19)) row.Localization.ResourceHelper = Text(19);
            if (values.ContainsKey(20)) row.Localization.ResourcePlaceholder = Text(20);
        }
        if (values.ContainsKey(15)) row.Kod = Text(15) ?? "";
        if (values.ContainsKey(17)) row.Naziv = Text(17) ?? "";
        if (values.ContainsKey(18)) row.Opis = Text(18);
        var entry = db.Entry(row);
        foreach (var slot in values.Keys.Where(slot => slot is >= 1 and <= 11))
            entry.Property($"Slot{slot}").CurrentValue = values[slot];
    }
}
