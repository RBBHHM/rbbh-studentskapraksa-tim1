using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.PeriodLock;

// Actual HTTP/controller/repository/audit/DB authorization path. Identity,
// email delivery and unique InMemory stores are isolated; never live SQL/SSO.
public sealed class UnlockResponseHttpTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Guid _originalId = Guid.NewGuid();
    private readonly Mock<IEmailService> _mail = new(MockBehavior.Strict);

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(PeriodLockController).Assembly.FullName
        });
        builder.WebHost.UseTestServer(); builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(PeriodLockController).Assembly);
        var database = "unlock-response-storage-discovery-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddAuthentication("PeriodFixture")
            .AddScheme<AuthenticationSchemeOptions, IdentityFixture>("PeriodFixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<IPeriodLockRepository, PeriodLockRepository>();
        builder.Services.AddScoped<IUnlockRequestRepository, UnlockRequestRepository>();
        builder.Services.AddScoped<IPeriodLockService, PeriodLockService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        // Unused user-list dependency must not start backend Program/Keycloak.
        var users = new Mock<IAppUserService>(MockBehavior.Strict);
        users.Setup(service => service.GetUsersAsync(1, 500, null, null)).ReturnsAsync(
            RBBH.ConnectedParties.Exceptions.Validations.Result<RBBH.ConnectedParties.DL.DTO.Users.GetUsersResponseDTO>.Success(new() { Users = [] }));
        builder.Services.AddSingleton(users.Object);
        _mail.Setup(service => service.SendUserResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mail.Setup(service => service.SendNeedsInfoNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        builder.Services.AddSingleton(_mail.Object); // No SMTP transport exists.
        _app = builder.Build(); _app.UseAuthentication(); _app.UseAuthorization(); _app.MapControllers();
        await _app.StartAsync(); _client = _app.GetTestClient();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var user = new AppUser { Email = "period-fixture@example.test", Username = "period-fixture", KeycloakId = Guid.NewGuid().ToString() };
        db.AppUsers.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Administrator)).Id });
        db.UnlockRequests.Add(new UnlockRequest
        {
            Id = _originalId,
            RequestedBy = "original-fixture",
            RequestedByEmail = "original@example.test",
            Year = 2026,
            Month = 10,
            Reason = "Synthetic original request",
            Status = "NEEDS_INFO",
            CreatedBy = "fixture"
        });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(490, true)]
    [InlineData(491, false)]
    [InlineData(500, false)]
    public async Task ApprovedBoundariesPreserveTextAndStorageContract(int length, bool accepted)
    {
        var message = new string('x', length);
        var response = await _client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{_originalId}/respond", new { message });
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var stored = await db.UnlockRequests.AsNoTracking().Where(request => request.Id != _originalId).ToListAsync();
        if (!accepted)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("490", body); Assert.Contains("message", body); Assert.DoesNotContain(message, body);
            Assert.Empty(stored); Assert.Empty(await db.AuditLogs.ToListAsync());
            _mail.VerifyNoOtherCalls(); return;
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = Assert.Single(stored);
        Assert.Equal("[ODGOVOR] " + message, saved.Reason); // Exact full text, not truncation.
        Assert.Equal("PENDING", saved.Status); Assert.Equal("period-fixture", saved.RequestedBy);
        Assert.Equal("NEEDS_INFO", (await db.UnlockRequests.SingleAsync(request => request.Id == _originalId)).Status);
        var maximum = db.Model.FindEntityType(typeof(UnlockRequest))!.FindProperty(nameof(UnlockRequest.Reason))!.GetMaxLength();
        Assert.Equal(500, maximum);
        Assert.True(saved.Reason.Length <= maximum,
            $"Accepted HTTP {(int)response.StatusCode}: input={length}, persisted Reason={saved.Reason.Length}, EF/SQL maximum={maximum}. " +
            "The controller adds '[ODGOVOR] ' before storing; no live SQL was executed.");
        var audit = Assert.Single(await db.AuditLogs.ToListAsync());
        Assert.Equal("UNLOCK_REQUEST", audit.Action); Assert.Equal(_originalId.ToString(), audit.RecordId);
        Assert.Equal("period-fixture", audit.Username); Assert.Contains("user_response", audit.NewValues);
        _mail.Verify(service => service.SendUserResponseAsync(It.IsAny<string>(), "period-fixture", 2026, 10, message), Times.Once);
    }

    [Theory]
    [InlineData("         ")]
    [InlineData(" xxxxxxxxx ")]
    public async Task WhitespaceCannotBypassMeaningfulMinimum(string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{_originalId}/respond", new { message })).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.Equal(1, await db.UnlockRequests.CountAsync()); Assert.Empty(await db.AuditLogs.ToListAsync());
        _mail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsInfoResponseAndUnlockPreserveWorkflowAndAudit()
    {
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
            (await db.UnlockRequests.SingleAsync()).Status = "PENDING";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{_originalId}/request-info", new { note = "Clarification needed" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{_originalId}/respond", new { message = "Complete clarification" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/period-lock/lock", new { year = 2026, month = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/period-lock/unlock", new { year = 2026, month = 10 })).StatusCode);
        await using var finalScope = _app.Services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.Equal("NEEDS_INFO", (await finalDb.UnlockRequests.SingleAsync(request => request.Id == _originalId)).Status);
        Assert.Equal("APPROVED", (await finalDb.UnlockRequests.SingleAsync(request => request.Id != _originalId)).Status);
        Assert.False((await finalDb.PeriodLocks.SingleAsync()).IsLocked);
        Assert.Equal(new[] { "UNLOCK_REQUEST_NEEDS_INFO", "UNLOCK_REQUEST", "PERIOD_LOCK", "PERIOD_UNLOCK" }, await finalDb.AuditLogs.OrderBy(log => log.Timestamp).Select(log => log.Action).ToArrayAsync());
    }

    [Fact]
    public async Task ForgedProviderGrantsCannotAuthorizeResponseForAnUnmappedIdentity()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Unmapped", "true");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{_originalId}/respond", new { message = "Complete clarification" })).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().UnlockRequests.CountAsync());
    }

    public async Task DisposeAsync() { _client.Dispose(); await _app.DisposeAsync(); }
    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new(
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Email, Request.Headers.ContainsKey("X-Test-Unmapped") ? "ordinary@example.test" : "period-fixture@example.test"),
                new Claim("email_verified", "true"), new Claim(ClaimTypes.Name, "period-fixture"), new Claim(ClaimTypes.Role, "admin"),
                new Claim("permission", ApplicationPermissions.CapitalCreate), new Claim("rct_done", "1")
            ], "PeriodFixture")), "PeriodFixture")));
    }
}
