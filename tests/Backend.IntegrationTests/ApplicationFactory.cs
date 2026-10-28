using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests
{
    internal class ApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace the real DB context with one pointing at the test container
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ConnectedPartiesDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ConnectedPartiesDbContext>(options =>
                    options.UseInMemoryDatabase($"api-tests-{Guid.NewGuid()}"));

            });
        }

    }
}
