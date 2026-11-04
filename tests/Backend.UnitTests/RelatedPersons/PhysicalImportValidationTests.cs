using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Exceptions;
using UnitTests.Mocks.DB;

namespace UnitTests.RelatedPersons;

public sealed class PhysicalImportValidationTests
{
    [Fact]
    public async Task NonemptyRowWithoutBothNamesIsReportedInsteadOfSilentlyIgnored()
    {
        await using var db = InMemoryContextFactory.Create();
        var service = new RelatedPersonService(db, Mock.Of<IEmailService>(), Options.Create(new EmailSettings()));
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");
        string[] headers = ["Ime", "Prezime", "Rezidentnost", "JMBG", "Broj pasoša", "FBA ID", "Osnov povezanosti",
            "Osnov posebnog odnosa", "Datum od", "Datum do", "Izjava bez članova porodice", "Pov. lice sa Bankom",
            "Lice u posebnom odnosu sa Bankom", "Poseban ugovor", "Malus & Clawback"];
        for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
        sheet.Cell(2, 3).Value = "Rezident";
        sheet.Cell(2, 4).Value = "1111111111111";
        using var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0;
        var result = await service.ImportFromExcelAsync(stream, "fixture");
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains("Ime je obavezno", Assert.Single(result.Errors));
        Assert.Empty(db.RelatedPersons);
    }

    [Fact]
    public async Task CorruptWorkbookHasSafeFileValidationAndDoesNotPersist()
    {
        await using var db = InMemoryContextFactory.Create();
        var service = new RelatedPersonService(db, Mock.Of<IEmailService>(), Options.Create(new EmailSettings()));
        using var stream = new MemoryStream("not-an-xlsx-private-marker"u8.ToArray());
        var error = await Assert.ThrowsAsync<ValidationException>(() => service.ImportFromExcelAsync(stream, "fixture"));
        Assert.Equal("file", error.Field);
        Assert.DoesNotContain("private-marker", error.Message);
        Assert.Empty(db.RelatedPersons);
    }
}
