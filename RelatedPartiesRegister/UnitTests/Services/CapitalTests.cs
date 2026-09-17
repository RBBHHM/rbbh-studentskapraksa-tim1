using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Capital;
using RBBH.ConnectedParties.DL.Entities.Capital;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Persistence;

namespace UnitTests.Services;

public sealed class CapitalTests
{
    private static ConnectedPartiesDbContext NewDb() => new(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task OneSharedCapitalIsProjectedOntoEveryLimit()
    {
        await using var db = NewDb();
        db.Capitals.Add(new Capital { OsnovniKapital = 100, RegulatorniKapital = 200, DopunskiKapital = 50, DatumKapitala = DateTime.UtcNow.Date, CreatedBy = "test" });
        db.Limiti.AddRange(new Limit { Naziv = "A", TipLimita = "MM", CreatedBy = "test", OsnovniKapital = 999 },
            new Limit { Naziv = "B", TipLimita = "OVL", CreatedBy = "test", RegulatorniKapital = 999 });
        await db.SaveChangesAsync();

        var result = await new LimitService(db).GetAll();
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, x => { Assert.Equal(100, x.OsnovniKapital); Assert.Equal(200, x.RegulatorniKapital); Assert.Equal(DateTime.UtcNow.Date, x.DatumKapitala); });
    }

    [Fact]
    public async Task CapitalRejectsMissingFieldsAndDuplicateDate()
    {
        await using var db = NewDb();
        var controller = new CapitalController(db);
        var date = DateTime.UtcNow.Date;
        var invalid = await controller.Create(new SaveCapitalDTO { DatumKapitala = date });
        Assert.IsType<BadRequestObjectResult>(invalid.Result);
        var valid = new SaveCapitalDTO { DatumKapitala = date, OsnovniKapital = 100, RegulatorniKapital = 200, DopunskiKapital = 50 };
        Assert.IsType<CreatedResult>((await controller.Create(valid)).Result);
        Assert.IsType<ConflictObjectResult>((await controller.Create(valid)).Result);
        Assert.Single(await controller.GetAll());
    }

    [Fact]
    public async Task MostRecentEffectiveCapitalAppliesWithoutFutureDate()
    {
        await using var db = NewDb();
        db.Capitals.AddRange(
            new Capital { OsnovniKapital = 10, RegulatorniKapital = 20, DatumKapitala = DateTime.UtcNow.Date.AddDays(-1), CreatedBy = "test" },
            new Capital { OsnovniKapital = 30, RegulatorniKapital = 40, DatumKapitala = DateTime.UtcNow.Date.AddDays(1), CreatedBy = "test" });
        db.Limiti.Add(new Limit { Naziv = "A", TipLimita = "MM", CreatedBy = "test" });
        await db.SaveChangesAsync();
        var result = await new LimitService(db).GetAll();
        Assert.Equal(10, result.Value!.Single().OsnovniKapital);
    }

    [Fact]
    public async Task CapitalExportUsesRegulatoryReportLayout()
    {
        await using var db = NewDb();
        db.Capitals.Add(new Capital { OsnovniKapital = 100, RegulatorniKapital = 200, DopunskiKapital = 50, DatumKapitala = new DateTime(2026, 9, 1), CreatedBy = "test" });
        await db.SaveChangesAsync();

        var file = Assert.IsType<FileContentResult>(await new CapitalController(db).Export());
        using var stream = new MemoryStream(file.FileContents);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Kapital");
        Assert.Equal("Kapital", sheet.Cell(1, 1).GetString());
        Assert.StartsWith("Generisano:", sheet.Cell(2, 1).GetString());
        Assert.Equal("Osnovni kapital", sheet.Cell(3, 2).GetString());
        Assert.Equal(100m, sheet.Cell(4, 2).GetValue<decimal>());
        Assert.Equal("Amalia", sheet.Cell(3, 2).Style.Font.FontName);
    }
}
