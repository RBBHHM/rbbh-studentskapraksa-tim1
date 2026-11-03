using System.Net;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.IoC.Extensions.HTTPMetricsExtension;

namespace IntegrationTests.Tests.Operations;

public sealed class MetricsAuthorizationHttpTests : AuthenticatedApiFixture
{
    protected override void ConfigureEndpoints(WebApplication app) => app.AddHTTPMetricsExtension();

    [Fact]
    public async Task MetricsUsesDatabaseAdministrativeReadPermissions()
    {
        using var anonymous = NewClient("unused"); anonymous.DefaultRequestHeaders.Add("X-Fixture-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/metrics")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/metrics")).StatusCode);
        using var guest = NewClient("unknown-metrics-reader");
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/metrics")).StatusCode);
        await InDatabase(async db =>
        {
            var actor = await db.AppUsers.SingleAsync(user => user.KeycloakId == "creator");
            db.UserRoles.RemoveRange(db.UserRoles.Where(row => row.UserId == actor.Id)); await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.GetAsync("/metrics")).StatusCode);
    }
}
