using ClosedXML.Excel;

namespace RBBH.ConnectedParties.Helpers.Excel;

/// <summary>Creates consistently styled RBI workbooks for registry exports.</summary>
public static class RegistryExcelExporter
{
    public static byte[] Create(
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SafeSheetName(sheetName));

        for (var column = 0; column < headers.Count; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < headers.Count; column++)
                SetValue(sheet.Cell(rowNumber, column + 1), column < row.Count ? row[column] : null);
            rowNumber++;
        }

        var header = sheet.Range(1, 1, 1, headers.Count);
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE600");
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.FromHtml("#1B1B1B");
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 26;
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns().AdjustToContents(12, 48);
        sheet.Style.Font.FontName = "Arial";
        sheet.Style.Font.FontSize = 10;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case DateTime date:
                cell.Value = date;
                cell.Style.DateFormat.Format = "dd.MM.yyyy";
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.DateTime;
                cell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                break;
            case bool boolean:
                cell.Value = boolean ? "Da" : "Ne";
                break;
            case decimal decimalValue:
                cell.Value = decimalValue;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case int integer:
                cell.Value = integer;
                break;
            default:
                cell.Value = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                break;
        }
    }

    private static string SafeSheetName(string value)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var safe = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        return safe.Length > 31 ? safe[..31] : safe;
    }
}
