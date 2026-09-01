using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RBBH.ConnectedParties.Helpers.Utils;

namespace RBBH.ConnectedParties.DL.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` works without starting the full app.
/// </summary>
public class ConnectedPartiesDbContextFactory : IDesignTimeDbContextFactory<ConnectedPartiesDbContext>
{
    public ConnectedPartiesDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<ConnectedPartiesDbContext>();
        optionsBuilder.UseSqlServer(ConnectionHelper.BuildConnection(configuration));

        return new ConnectedPartiesDbContext(optionsBuilder.Options);
    }
}
