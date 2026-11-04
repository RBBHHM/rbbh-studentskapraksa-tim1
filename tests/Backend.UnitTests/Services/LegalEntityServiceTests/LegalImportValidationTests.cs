using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.Exceptions;
using UnitTests.Mocks.DB;

namespace UnitTests.Services.LegalEntityServiceTests;

public sealed class LegalImportValidationTests
{
    private static readonly string[] Headers = ["Naziv", "Tip", "Porezni broj", "FBA ID", "Matbroj",
        "Osnov povezanosti", "Opis povezanosti", "Pov. lice sa Bankom", "Datum od", "Datum do"];

    public static TheoryData<string, string, bool> Fields => new()
    {
        { "name", new string('Ž', 100), true },
        { "name", new string('Ž', 101), false },
        { "matbroj", "12345", true },
        { "matbroj", "12A", false },
        { "matbroj", new string('1', 50), true },
        { "matbroj", new string('1', 51), false },
        { "matbroj", "١٢٣", false },
        { "description", new string('a', 500), true },
        { "description", new string('a', 501), false },
        { "fbaId", "١٢٣", false },
        { "fbaId", "12345678901", false }
    };

    [Theory]
    [MemberData(nameof(Fields))]
    public async Task ImportUsesTheSameFieldBoundsAsCreateAndNeverTruncates(string field, string value, bool accepted)
    {
        using var db = InMemoryContextFactory.Create();
        var service = new LegalEntityService(db, NullLogger<LegalEntityService>.Instance);
        var dto = Valid();
        switch (field)
        {
            case "name": dto.Name = value; break;
            case "matbroj": dto.Matbroj = value; break;
            case "description": dto.ConnectionDescription = value; break;
            case "fbaId": dto.FbaId = value; break;
        }
        using var stream = Workbook(dto);
        var imported = await service.ImportFromExcelAsync(stream, "fixture");
        Assert.Equal(accepted ? 1 : 0, imported.Imported);
        Assert.Equal(accepted ? 0 : 1, imported.Failed);
        if (accepted)
        {
            var stored = Assert.Single(db.LegalEntities);
            Assert.Equal(dto.Name, stored.Name);
            Assert.Equal(dto.Matbroj, stored.Matbroj);
            Assert.Equal(dto.ConnectionDescription, stored.ConnectionDescription);
        }
        else
        {
            Assert.Empty(db.LegalEntities);
            Assert.Contains("Red 2:", Assert.Single(imported.Errors));
            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto, "fixture"));
            Assert.Empty(db.LegalEntities);
            var original = await service.CreateAsync(Valid(), "fixture");
            var update = new UpdateLegalEntityDTO
            {
                Name = dto.Name, IsResident = dto.IsResident, FbaId = dto.FbaId, Matbroj = dto.Matbroj,
                BasisOfConnection = dto.BasisOfConnection, ConnectionDescription = dto.ConnectionDescription,
                ConnectedWithBank = dto.ConnectedWithBank, DateFrom = dto.DateFrom
            };
            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(original.Id, update, "fixture"));
            Assert.Equal(Valid().Name, Assert.Single(db.LegalEntities).Name);
        }
    }

    [Fact]
    public async Task IncompleteNamedRowIsCountedAsFailedWhileAnotherValidRowIsPersisted()
    {
        using var db = InMemoryContextFactory.Create();
        var service = new LegalEntityService(db, NullLogger<LegalEntityService>.Instance);
        var missingName = Valid(); missingName.Name = "";
        var valid = Valid(); valid.FbaId = "999";
        using var stream = Workbook(missingName, valid);
        var result = await service.ImportFromExcelAsync(stream, "fixture");
        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains("Naziv", Assert.Single(result.Errors));
        Assert.Equal("999", Assert.Single(db.LegalEntities).FbaId);
    }

    [Theory]
    [InlineData("not-an-excel-file private-marker")]
    [InlineData("PK\u0003\u0004truncated private-marker")]
    public async Task InvalidWorkbookReturnsBusinessValidationWithoutInternalParserDetails(string content)
    {
        using var db = InMemoryContextFactory.Create();
        var service = new LegalEntityService(db, NullLogger<LegalEntityService>.Instance);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var error = await Assert.ThrowsAsync<ValidationException>(() => service.ImportFromExcelAsync(stream, "fixture"));
        Assert.Equal("file", error.Field);
        Assert.DoesNotContain("private-marker", error.Message);
        Assert.Empty(db.LegalEntities);
    }

    private static CreateLegalEntityDTO Valid() => new()
    {
        Name = "Kompanija Ž", IsResident = false, FbaId = "123", Matbroj = "456",
        BasisOfConnection = "VL", ConnectionDescription = "Opis povezanosti", ConnectedWithBank = true,
        DateFrom = new DateTime(2026, 1, 1)
    };

    private static MemoryStream Workbook(params CreateLegalEntityDTO[] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");
        for (var column = 0; column < Headers.Length; column++) sheet.Cell(1, column + 1).Value = Headers[column];
        for (var row = 0; row < rows.Length; row++)
        {
            var dto = rows[row];
            string[] values = [dto.Name, "Nerezident", "", dto.FbaId!, dto.Matbroj!, dto.BasisOfConnection,
                dto.ConnectionDescription!, "DA", "01.01.2026", ""];
            for (var column = 0; column < values.Length; column++) sheet.Cell(row + 2, column + 1).Value = values[column];
        }
        var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0; return stream;
    }
}
