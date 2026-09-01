using ClosedXML.Excel;
using FluentAssertions;
using RBBH.ConnectedParties.Helpers.Excel;

namespace UnitTests.Helpers;

public sealed class ExcelImportSchemaTests
{
    private static readonly ExcelImportSchema.Column[] Columns =
    [
        new("Naziv / Name", "Naziv", "Name"),
        new("Tip / Type", "Tip", "Type")
    ];

    [Fact]
    public void Validate_RejectsUnrelatedWorkbook()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Random");
        worksheet.Cell(1, 1).Value = "Completely unrelated";
        worksheet.Cell(1, 2).Value = "Spreadsheet";
        worksheet.Cell(2, 1).Value = "Some value";

        var errors = ExcelImportSchema.Validate(worksheet, Columns);

        errors.Should().Contain(error => error.Contains("neispravno zaglavlje"));
    }

    [Fact]
    public void Validate_AcceptsApprovedEnglishAliases()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Import");
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Type";
        worksheet.Cell(2, 1).Value = "Example";
        worksheet.Cell(2, 2).Value = "Resident";

        ExcelImportSchema.Validate(worksheet, Columns).Should().BeEmpty();
    }

    [Fact]
    public void Validate_RejectsHeaderWithoutDataRows()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Import");
        worksheet.Cell(1, 1).Value = "Naziv";
        worksheet.Cell(1, 2).Value = "Tip";

        ExcelImportSchema.Validate(worksheet, Columns)
            .Should().Contain(error => error.Contains("nema nijedan red podataka"));
    }
}
