using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace RBBH.ConnectedParties.Helpers.Excel;

/// <summary>
/// Validates an import workbook before any row is persisted. Header aliases
/// support the approved Bosnian and English templates while an unrelated
/// spreadsheet is rejected with actionable feedback.
/// </summary>
public static class ExcelImportSchema
{
    public const int MaximumDataRows = 5_000;

    public sealed record Column(string DisplayName, params string[] AcceptedHeaders);

    public static IReadOnlyList<string> Validate(IXLWorksheet worksheet, IReadOnlyList<Column> columns)
    {
        var errors = new List<string>();
        var lastUsedColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (lastUsedRow == 0)
            return ["Excel datoteka je prazna. Koristite propisani predložak sa zaglavljem i podacima."];

        if (lastUsedColumn != columns.Count)
        {
            errors.Add(
                $"Excel predložak mora sadržavati tačno {columns.Count} kolona, a pronađeno je {lastUsedColumn}. " +
                "Nemojte dodavati, uklanjati niti premještati kolone.");
        }

        for (var index = 0; index < columns.Count; index++)
        {
            var actual = worksheet.Cell(1, index + 1).GetString().Trim();
            var expected = columns[index];
            if (expected.AcceptedHeaders.Any(alias => Normalize(alias) == Normalize(actual)))
                continue;

            errors.Add(
                $"Kolona {XLHelper.GetColumnLetterFromNumber(index + 1)} ima neispravno zaglavlje. " +
                $"Očekivano je '{expected.DisplayName}', a pronađeno '{(actual.Length == 0 ? "prazno" : actual)}'.");
        }

        var dataRows = Math.Max(lastUsedRow - 1, 0);
        if (dataRows == 0)
            errors.Add("Excel datoteka nema nijedan red podataka ispod zaglavlja.");
        else if (dataRows > MaximumDataRows)
            errors.Add($"Excel datoteka sadrži {dataRows} redova. Dozvoljeno je najviše {MaximumDataRows} redova po uvozu.");

        return errors;
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString();
    }
}
