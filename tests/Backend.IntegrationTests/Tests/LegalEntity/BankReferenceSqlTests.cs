using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IntegrationTests.Tests.Users;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class BankReferenceSqlTests : IAsyncLifetime
{
    private ConnectedPartiesDbContext _db = null!;
    private BankReferenceOptions _options = null!;
    public async Task InitializeAsync()
    {
        var baseConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable);
        if (string.IsNullOrEmpty(baseConnection)) return;
        var connection = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = "RBBH_BANK_ADAPTER_" + Guid.NewGuid().ToString("N") };
        _db = new(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        await _db.Database.EnsureCreatedAsync();
        await _db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE FixtureCustomers (Tax varchar(13),Matbroj varchar(50),Name nvarchar(200) COLLATE Latin1_General_100_CS_AS);
            CREATE TABLE FixtureGroups (Number varchar(50),Name nvarchar(200) COLLATE Latin1_General_100_CS_AS);
            INSERT FixtureCustomers VALUES ('1234567890123','001',N'Customer Mixed Case'),('2222222222222','002',N'Literal % sign');
            INSERT FixtureGroups VALUES ('123',N'Group Mixed Case'),('124',N'Second'),('125',N'Third');
            """);
        _options = new() { Enabled = true, ConnectionString = connection.ConnectionString, CustomerTable = "dbo.FixtureCustomers",
            CustomerTaxColumn = "Tax", CustomerMatbrojColumn = "Matbroj", CustomerNameColumn = "Name", GccTable = "dbo.FixtureGroups", GccNumberColumn = "Number", GccNameColumn = "Name" };
    }
    private SqlBankReferenceSource Source() => new(Options.Create(_options), NullLogger<SqlBankReferenceSource>.Instance);
    [SqlServerFact]
    public async Task RealParameterizedQuerySupportsCaseInsensitiveSearchLiteralWildcardsAndPaging()
    {
        var customers = await Source().Customers("mixed case", 1, 20, default); Assert.Single(customers.Items); Assert.Equal("001", customers.Items[0].Matbroj);
        var literal = await Source().Customers("%", 1, 20, default); Assert.Single(literal.Items); Assert.Equal("2222222222222", literal.Items[0].TaxNumber);
        var page = await Source().Groups(null, 2, 2, default); Assert.Equal(3, page.Total); Assert.Single(page.Items); Assert.Equal("125", page.Items[0].Number);
        Assert.Empty((await Source().Customers("' OR 1=1;--", 1, 20, default)).Items);
    }
    [SqlServerFact]
    public async Task ExactGccLookupRejectsAmbiguousSourceAndUnsafeIdentifierConfiguration()
    {
        Assert.Equal("Group Mixed Case", (await Source().Group("123", default))!.Name);
        await _db.Database.ExecuteSqlRawAsync("INSERT FixtureGroups VALUES ('123',N'Conflicting group')");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Group("123", default))).Status);
        _options.GccTable = "FixtureGroups; DROP TABLE FixtureCustomers";
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Group("123", default))).Status);
        await using var command = _db.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM FixtureCustomers";
        await command.Connection!.OpenAsync(); Assert.Equal(2, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }
    [SqlServerFact]
    public async Task MissingMappingAndUnconfiguredSourceFailSafelyRatherThanSupplyingFakeValues()
    {
        _options.CustomerTaxColumn = "";
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Customers(null, 1, 20, default)); Assert.Equal(503, error.Status); Assert.DoesNotContain(_options.ConnectionString, error.Message);
        _options.Enabled = false; Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Source().Groups(null, 1, 20, default))).Status);
    }
    public async Task DisposeAsync() { if (_db is not null) { await _db.Database.EnsureDeletedAsync(); await _db.DisposeAsync(); } }
}
