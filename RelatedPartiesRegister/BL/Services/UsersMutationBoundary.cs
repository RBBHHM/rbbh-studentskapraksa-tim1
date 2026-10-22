using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

// Serialize only access-reducing user operations. The SQL transaction-owned
// application lock coordinates all API replicas without a schema change.
internal static class UsersMutationBoundary
{
    private static readonly SemaphoreSlim LocalStoreGate = new(1, 1);

    internal static async Task<T> ExecuteAsync<T>(ConnectedPartiesDbContext db,
        Func<Task<T>> mutation, Func<T> busy, CancellationToken cancellationToken)
    {
        if (db.Database.IsInMemory())
        {
            // The existing local Development/test store has no transactions.
            await LocalStoreGate.WaitAsync(cancellationToken);
            try { db.ChangeTracker.Clear(); return await mutation(); }
            finally { LocalStoreGate.Release(); }
        }

        // Fail closed for unconfigured providers rather than using a process-only
        // lock in production. This application currently uses SQL Server.
        if (!db.Database.IsSqlServer()) return busy();
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = N'RBBH.Users.ActiveAdminSafety',
                    @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 0;
                SELECT @result;
                """;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not int lockResult || lockResult < 0) return busy();
            var response = await mutation();
            await transaction.CommitAsync(cancellationToken);
            return response;
        });
    }
}
