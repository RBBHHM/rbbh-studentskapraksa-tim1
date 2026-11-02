using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.PeriodLock;

public sealed class PeriodOwnershipHttpTests : AuthenticatedApiFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUnlockRequestRepository, UnlockRequestRepository>();
        services.AddScoped<IPeriodLockService, PeriodLockService>();
        services.AddSingleton(Mock.Of<IAppUserService>());
        services.AddSingleton<EmailLogStore>();
    }

    private async Task<Guid> SeedRequest(string owner, string status = "NEEDS_INFO")
    {
        var row = new UnlockRequest { RequestedBySubject = owner, RequestedBy = owner,
            RequestedByEmail = owner + "@example.test", Reason = "Private request reason", Status = status,
            Year = 2026, Month = 10, CreatedBy = "fixture" };
        await InDatabase(async db => { db.UnlockRequests.Add(row); await db.SaveChangesAsync(); });
        return row.Id;
    }

    [Fact]
    public async Task DefaultListIsSubjectScopedAndPrivilegedReviewUsesSeparateRoute()
    {
        await SeedRequest("creator"); await SeedRequest("reviewer");
        var mine = await Client.GetFromJsonAsync<JsonElement>("/api/period-lock/unlock-requests");
        Assert.Equal(1, mine.GetProperty("total").GetInt32());
        Assert.Equal("creator", mine.GetProperty("requests")[0].GetProperty("requestedBy").GetString());
        var review = await Client.GetFromJsonAsync<JsonElement>("/api/period-lock/unlock-requests/review");
        Assert.Equal(2, review.GetProperty("total").GetInt32());
        using var guest = NewClient("unknown");
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/api/period-lock/unlock-requests/review")).StatusCode);
    }

    [Fact]
    public async Task EvenAdminCannotRespondToAnotherSubjectsRequest()
    {
        var id = await SeedRequest("reviewer");
        var response = await Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", new { message = "Unauthorized response" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await InDatabase(async db => { Assert.Equal(1, await db.UnlockRequests.CountAsync()); Assert.Empty(db.AuditLogs); });
    }

    [Fact]
    public async Task OwnerCanRespondAfterEmailChangeExactlyOnce()
    {
        var id = await SeedRequest("creator");
        Client.DefaultRequestHeaders.Add("X-Email", "changed@example.test");
        var payload = new { message = "Additional response text", requestedBySubject = "forged" };
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", payload)).StatusCode);
        await InDatabase(async db =>
        {
            var child = Assert.Single(await db.UnlockRequests.Where(row => row.ParentRequestId == id).ToListAsync());
            Assert.Equal("creator", child.RequestedBySubject); Assert.Equal("changed@example.test", child.RequestedByEmail);
            Assert.Equal("NEEDS_INFO", (await db.UnlockRequests.FindAsync(id))!.Status);
            Assert.Single(db.AuditLogs);
        });
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("APPROVED")]
    [InlineData("REJECTED")]
    public async Task ResponseIsNotAcceptedOutsideNeedsInfo(string state)
    {
        var id = await SeedRequest("creator", state);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", new { message = "Additional response text" })).StatusCode);
        await InDatabase(async db => { Assert.Equal(1, await db.UnlockRequests.CountAsync()); Assert.Empty(db.AuditLogs); });
    }

    [Fact]
    public async Task CapitalRetainsRequestProcessingPermission()
    {
        await InDatabase(async db =>
        {
            var user = new AppUser { Username = "capital", Email = "capital@example.test", KeycloakId = "capital", KeycloakSubjectConfirmed = true };
            db.AppUsers.Add(user);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Capital)).Id });
            await db.SaveChangesAsync();
        });
        var id = await SeedRequest("creator", "PENDING");
        using var capital = NewClient("capital");
        Assert.Equal(HttpStatusCode.OK, (await capital.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/reject", new { note = "Capital review rejection" })).StatusCode);
    }

    [Fact]
    public async Task ProductionMailPreviewDoesNotExposeStoredRecipientOrBody()
    {
        App.Services.GetRequiredService<EmailLogStore>().Add(new EmailLogEntry { To = "reviewer@example.test", HtmlBody = "private-mail-body" });
        var response = await Client.GetAsync("/api/email-log");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("private-mail-body", await response.Content.ReadAsStringAsync());
    }
}
