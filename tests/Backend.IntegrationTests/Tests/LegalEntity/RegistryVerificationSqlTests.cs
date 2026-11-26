using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class RegistryVerificationSqlTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;
    private readonly VerificationBarrier _barrier = new();
    protected override void ConfigureDatabase(DbContextOptionsBuilder options) => options.AddInterceptors(_barrier);

    private async Task<Guid> CreateLegal()
    {
        var response = await Client.PostAsJsonAsync("/api/legal-entities", RegistryVerificationHttpTests.Legal());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [SqlServerFact]
    public async Task SimultaneousVerificationsCommitExactlyOneAudit()
    {
        var id = await CreateLegal();
        using var first = NewClient("reviewer"); using var second = NewClient("admin2");
        _barrier.Enabled = true;
        var responses = await Task.WhenAll(first.PostAsync($"/api/verification/legal-person/{id}", null),
            second.PostAsync($"/api/verification/legal-person/{id}", null));
        _barrier.Enabled = false;
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await InDatabase(async db => Assert.Single(await db.AuditLogs.Where(row => row.Action == "VERIFY").ToListAsync()));
    }

    [SqlServerFact]
    public async Task DatabaseAuditFailureRollsBackVerificationStatus()
    {
        var id = await CreateLegal();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FixtureRejectVerificationAudit ON AuditLogs AFTER INSERT AS
            BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'VERIFY') THROW 51000, 'fixture audit failure', 1; END
            """));
        using var reviewer = NewClient("reviewer");
        Assert.Equal(HttpStatusCode.InternalServerError, (await reviewer.PostAsync($"/api/verification/legal-person/{id}", null)).StatusCode);
        await InDatabase(async db =>
        {
            Assert.Equal("Draft", (await db.LegalEntities.FindAsync(id))!.Status);
            Assert.Empty(await db.AuditLogs.Where(row => row.Action == "VERIFY").ToListAsync());
        });
    }

    [SqlServerFact]
    public async Task AdditiveUpgradePreservesHistoricalRecordWithoutInventingCreator()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_PROVENANCE_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261026115804_ConfirmKeycloakSubjects");
            var id = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO LegalEntities (Id, IsResident, Name, BasisOfConnection, Status, CreatedBy, CreatedAt, IsActive) VALUES ({id}, {false}, {"Historical company"}, {"VL"}, {"Draft"}, {"historical@example.test"}, {DateTime.UtcNow}, {true})");
            await db.GetService<IMigrator>().MigrateAsync("20261124101000_EnsureUniqueUserIdentityAndUpdateRoleRequestCopy");
            var provenId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO LegalEntities (Id, IsResident, FbaId, Name, BasisOfConnection, Status, CreatedBy, CreatedAt, IsActive, CreatorSubject)
                VALUES ({provenId}, {false}, {"145"}, {"Known company"}, {"VL"}, {"Draft"}, {"original"}, {DateTime.UtcNow}, {true}, {"opaque-subject"})
                """);
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.LegalEntities ADD [Version] rowversion;");
            await db.Database.MigrateAsync();

            var origin = await db.LegalEntityVerificationOrigins.SingleAsync();
            Assert.Equal(provenId, origin.LegalEntityId);
            Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.Unicode.GetBytes("opaque-subject"))), origin.Fingerprint);
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value] FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.LegalEntities') AND name IN (N'CreatorSubject', N'ConnectionDescription', N'Version')
                """).SingleAsync());
            var row = await db.LegalEntities.SingleAsync(entity => entity.Id == id);
            Assert.Equal("Historical company", row.Name);
            Assert.Equal("historical@example.test", row.CreatedBy);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private sealed class VerificationBarrier : SaveChangesInterceptor
    {
        public bool Enabled;
        private int _arrived;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State == EntityState.Added && entry.Entity.Action == "VERIFY"))
            {
                if (Interlocked.Increment(ref _arrived) == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
