using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Infrastructure;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"integration-tests-{Guid.NewGuid()}";
    public bool IsAvailable => true;
    public string SkipReason => string.Empty;

    public ConnectedPartiesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        return new ConnectedPartiesDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Provider-neutral integration";
}
