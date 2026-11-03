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

    public static XLWorkbook OpenWorkbook(Stream stream)
    {
        try
        {
            WorkbookEnvelope.Validate(stream);
            return new XLWorkbook(stream);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            throw new RBBH.ConnectedParties.Exceptions.ValidationException("file",
                BackendMessages.Message("workbook.invalid"));
        }
    }

    public static string CellText(IXLCell cell) => cell.DataType == XLDataType.DateTime
        ? cell.GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
        : cell.GetString().Trim();

    public sealed record Column(string DisplayName, params string[] AcceptedHeaders);

    public static IReadOnlyList<string> Validate(IXLWorksheet worksheet, IReadOnlyList<Column> columns)
    {
        var errors = new List<string>();
        var lastUsedColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (lastUsedRow == 0)
            return [BackendMessages.Message("workbook.empty")];

        if (lastUsedColumn != columns.Count)
        {
            errors.Add(
                BackendMessages.Message("workbook.schema.count", columns.Count, lastUsedColumn));
        }

        for (var index = 0; index < columns.Count; index++)
        {
            var header = worksheet.Cell(1, index + 1);
            if (header.HasFormula) { errors.Add(BackendMessages.Message("workbook.header.formula")); continue; }
            var actual = header.GetString().Trim();
            var expected = columns[index];
            if (expected.AcceptedHeaders.Any(alias => Normalize(alias) == Normalize(actual)))
                continue;

            errors.Add(
                BackendMessages.Message("workbook.schema.header", XLHelper.GetColumnLetterFromNumber(index + 1), expected.DisplayName, actual.Length == 0 ? BackendMessages.Message("workbook.value.empty") : actual));
        }

        var dataRows = Math.Max(lastUsedRow - 1, 0);
        if (dataRows == 0)
            errors.Add(BackendMessages.Message("workbook.data.empty"));
        else if (dataRows > MaximumDataRows)
            errors.Add(BackendMessages.Message("workbook.data.limit", dataRows, MaximumDataRows));

        if (lastUsedRow <= MaximumDataRows + 1 && lastUsedColumn <= columns.Count &&
            worksheet.CellsUsed().Any(cell => cell.HasFormula))
            errors.Add(BackendMessages.Message("workbook.data.formula"));
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
