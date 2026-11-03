using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace IntegrationTests.Tests;

public sealed class LimitCommitAckSqlTests : LimitImportFixture
{
    protected override bool UseSql => true;
    private readonly LostCommitAck _failure = new();
    protected override void ConfigureDatabase(DbContextOptionsBuilder options) =>
        options.ReplaceService<IExecutionStrategyFactory, RetryFactory>().AddInterceptors(_failure);
    [SqlServerFact]
    public async Task LostCommitAcknowledgementDoesNotDuplicateCreatedOrImportedLimits()
    {
        await Seed(); _failure.Arm = true; var id = await Create();
        await InDatabase(async db => { Assert.Single(db.Limiti); Assert.Equal(id.ToString(), (await db.AuditLogs.SingleAsync()).RecordId); });
        var bytes = await File(200); _failure.Arm = true;
        var response = await Import(bytes); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imported").GetInt32());
        await InDatabase(async db => { Assert.Equal(2, await db.Limiti.CountAsync()); Assert.Equal(2, await db.AuditLogs.CountAsync()); });
        Assert.Equal(2, _failure.Failures);
    }
    private sealed class CommitAckFailure : Exception;
    private sealed class LostCommitAck : DbTransactionInterceptor
    {
        public bool Arm;
        public int Failures;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!Arm) return Task.CompletedTask;
            Arm = false; Failures++; throw new CommitAckFailure(); // SQL COMMIT has already succeeded.
        }
    }
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new FixtureRetry(dependencies);
    }
    private sealed class FixtureRetry(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is CommitAckFailure;
    }
}
