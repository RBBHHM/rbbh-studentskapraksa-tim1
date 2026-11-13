using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Persistence;
using LegalEntity = RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity;
using Report = RBBH.ConnectedParties.DL.Entities.Report.Report;

namespace UnitTests.Services;

/// <summary>
/// SQL-generation regression guard: list queries must not hydrate complete
/// persistence entities and must not transmit report DataSnapshot JSON.
/// No SQL connection is opened and no database is modified.
/// </summary>
public sealed class SummaryProjectionSqlTests
{
    private static ConnectedPartiesDbContext Context()
    {
        var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseSqlServer("Server=localhost;Database=QueryProjectionOnly;User Id=test;Password=synthetic-only;TrustServerCertificate=True")
            .Options;
        return new ConnectedPartiesDbContext(options);
    }

    private static Expression<Func<TEntity, TDto>> Projection<TEntity, TDto>(Type owner, string field)
    {
        var value = owner.GetField(field, BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        return Assert.IsType<Expression<Func<TEntity, TDto>>>(value);
    }

    [Fact]
    public void ReportListSelectsSummaryColumnsWithoutLargePersistedJsonSnapshot()
    {
        using var db = Context();
        var projection = Projection<Report, ReportDTO>(typeof(ReportService), "ReportDtoProjection");
        var sql = db.Reports.AsNoTracking()
            .Where(report => report.ReportType == "DAILY")
            .OrderByDescending(report => report.ReportDate)
            .Select(projection)
            .ToQueryString();

        Assert.Contains("[ReportDate]", sql);
        Assert.Contains("[DataSource]", sql);
        Assert.False(sql.Contains("[DataSnapshot]", StringComparison.OrdinalIgnoreCase),
            "Report list query must not transfer DataSnapshot JSON from SQL Server.");
    }

    [Fact]
    public void LegalRegistryListProjectsOnlyPublicDtoColumnsNotAuditProvenance()
    {
        using var db = Context();
        var projection = Projection<LegalEntity, LegalEntityDTO>(typeof(LegalEntityService), "DtoProjection");
        var sql = db.LegalEntities.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.CreatedAt)
            .Take(20)
            .Select(projection)
            .ToQueryString();

        Assert.Contains("[Name]", sql);
        Assert.False(sql.Contains("[CreatorSubject]", StringComparison.OrdinalIgnoreCase),
            "Registry list must not hydrate provenance metadata that is absent from its DTO.");
    }
}
