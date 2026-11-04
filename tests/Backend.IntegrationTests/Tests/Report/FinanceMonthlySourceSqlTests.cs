using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;

namespace IntegrationTests.Tests.Report;

public sealed class FinanceMonthlySourceSqlTests : IAsyncLifetime
{
    private ConnectedPartiesDbContext _db = null!;
    private FinanceMonthlyOptions _options = null!;
    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable); if (string.IsNullOrEmpty(configured)) return;
        var connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = "RBBH_FINANCE_SOURCE_" + Guid.NewGuid().ToString("N") };
        _db = new(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options); await _db.Database.EnsureCreatedAsync();
        await _db.Database.ExecuteSqlRawAsync("CREATE TABLE FinanceFixture (Party varchar(50),Balance decimal(18,2) NULL,OffBalance decimal(18,2) NULL); INSERT FinanceFixture VALUES ('001',125.25,74.75),('002',10,20)");
        _options = new() { Enabled = true, ConnectionString = connection.ConnectionString, CustomerKeyColumn = "Party", BalanceColumn = "Balance", OffBalanceColumn = "OffBalance", Currency = "BAM",
            MonthlyTables = new() { ["2026-09"] = "dbo.FinanceFixture" } };
    }
    private SqlFinanceMonthlySource Source() => new(Options.Create(_options), NullLogger<SqlFinanceMonthlySource>.Instance);
    [SqlServerFact]
    public async Task ExactPeriodAndDocumentedSumPreserveDecimalValuesAndBatchOverFiveHundredKeys()
    {
        var rows = await Source().Read(2026, 9, new[] { "001", "002" }, default); Assert.Equal(200, rows[0].TotalExposure); Assert.Equal(30, rows[1].TotalExposure);
        await _db.Database.ExecuteSqlRawAsync("INSERT FinanceFixture SELECT TOP (501) CONVERT(varchar(50),ROW_NUMBER() OVER (ORDER BY object_id) + 1000),1,2 FROM sys.all_objects");
        var keys = Enumerable.Range(1001, 501).Select(value => value.ToString()).ToArray();
        var batch = await Source().Read(2026, 9, keys, default); Assert.Equal(501, batch.Count); Assert.All(batch, row => Assert.Equal(3, row.TotalExposure));
    }
    [SqlServerFact]
    public async Task MissingNullDuplicateAndUnmappedPeriodNeverBecomeZeroOrLocalFallback()
    {
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "999" }, default))).Status);
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 8, new[] { "001" }, default))).Status);
        await _db.Database.ExecuteSqlRawAsync("INSERT FinanceFixture VALUES ('003',NULL,20)");
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "003" }, default))).Status);
        await _db.Database.ExecuteSqlRawAsync("INSERT FinanceFixture VALUES ('001',1,2)");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "001" }, default))).Status);
    }
    [SqlServerFact]
    public async Task UnapprovedPrecisionAndCancellationAreNotHiddenOrRounded()
    {
        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE FinanceFixture ALTER COLUMN Balance decimal(18,3) NULL; INSERT FinanceFixture VALUES ('003',0.001,20)");
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "003" }, default))).Status);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Source().Read(2026, 9, new[] { "001" }, cancellation.Token));
    }

    [SqlServerFact]
    public async Task UnknownCurrencyUnsafeIdentifiersAndUnconfirmedKeyMappingFailClosed()
    {
        _options.Currency = "EUR"; Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "001" }, default))).Status);
        _options.Currency = "BAM"; _options.CustomerKeyColumn = ""; Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "001" }, default))).Status);
        _options.CustomerKeyColumn = "Party"; _options.MonthlyTables["2026-09"] = "FinanceFixture; DROP TABLE FinanceFixture";
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Read(2026, 9, new[] { "001" }, default))).Status);
    }
    public async Task DisposeAsync() { if (_db is not null) { await _db.Database.EnsureDeletedAsync(); await _db.DisposeAsync(); } }
}
