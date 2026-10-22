using System.Text.RegularExpressions;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// Presentation adapter for the existing string-only import result. Never echo
// an upstream header, exception or SQL message. No workbook parsing happens here.
public static class ImportFeedback
{
    public static string Format(string error, AppText text, IReadOnlyList<string> columnKeys)
    {
        error ??= string.Empty;
        var count = Regex.Match(error, @"^Excel predložak mora sadržavati tačno ([0-9]+) kolona, a pronađeno je ([0-9]+)\.");
        if (count.Success) return text.Format("import.column-count", count.Groups[1].Value, count.Groups[2].Value);
        var header = Regex.Match(error, @"^Kolona ([A-Z]{1,3}) ima neispravno zaglavlje\.");
        if (header.Success)
        {
            var column = header.Groups[1].Value;
            var index = column.Aggregate(0, (value, character) => value * 26 + character - 'A' + 1) - 1;
            if (index >= 0 && index < columnKeys.Count)
                return text.Format("import.header-error", column, text[columnKeys[index]]);
        }
        if (error.StartsWith("Excel datoteka je prazna.", StringComparison.Ordinal)) return text["import.empty-file"];
        if (error.StartsWith("Excel datoteka nema nijedan red podataka", StringComparison.Ordinal)) return text["import.no-rows"];
        var rows = Regex.Match(error, @"^Excel datoteka sadrži ([0-9]+) redova\. Dozvoljeno je najviše ([0-9]+)");
        if (rows.Success) return text.Format("import.row-limit", rows.Groups[1].Value, rows.Groups[2].Value);
        var row = Regex.Match(error, @"^(?:Red|Row)\s+([0-9]+):\s*(.*)$");
        if (row.Success)
        {
            var message = row.Groups[2].Value;
            var field = KnownField(message);
            if (field is { } known)
                return text.Format("import.row-field-error", row.Groups[1].Value, text[known.Field], text[known.Validation]);
            return text.Format("import.row-error", row.Groups[1].Value);
        }
        return text["import.schema-error"];
    }

    private static (string Field, string Validation)? KnownField(string message)
    {
        if (message == "Ime je obavezno.") return ("person.first-name", "validation.required");
        if (message == "Prezime je obavezno.") return ("person.last-name", "validation.required");
        if (message.StartsWith("Ime mora sadržavati", StringComparison.Ordinal)) return ("person.first-name", "validation.name");
        if (message.StartsWith("Prezime mora sadržavati", StringComparison.Ordinal)) return ("person.last-name", "validation.name");
        if (message == "Fizičko lice s ovim JMBG-om već postoji.") return ("person.jmbg", "validation.duplicate");
        if (message == "Fizičko lice s ovim FBA ID-em već postoji.") return ("person.fba", "validation.duplicate");
        if (message.StartsWith("JMBG ", StringComparison.Ordinal)) return ("person.jmbg", "validation.jmbg");
        if (message.StartsWith("Broj pasoša ", StringComparison.Ordinal)) return ("person.passport", "validation.passport");
        if (message.StartsWith("FBA ID ", StringComparison.Ordinal)) return ("person.fba", "validation.fba");
        if (message.StartsWith("Rezidentnost mora biti", StringComparison.Ordinal)) return ("person.residency", "validation.required");
        if (message.StartsWith("Datum do ne može", StringComparison.Ordinal)) return ("person.to", "validation.date-order");
        if (message.StartsWith("Datum početka", StringComparison.Ordinal)) return ("person.from", "validation.date");
        if (message.StartsWith("Datum završetka", StringComparison.Ordinal)) return ("person.to", "validation.date");
        if (message.StartsWith("Osnov povezanosti je", StringComparison.Ordinal)) return ("person.basis", "validation.required");
        if (message.StartsWith("Opis osnova povezanosti je", StringComparison.Ordinal)) return ("person.description", "validation.required");
        if (message.StartsWith("Osnov posebnog odnosa je", StringComparison.Ordinal)) return ("person.special-basis", "validation.required");
        return null;
    }
}
