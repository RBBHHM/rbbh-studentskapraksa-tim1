using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.Middlewares;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBI.ApiCore;

namespace Web.Blazor.BrowserHost;

// TEST-ONLY executable. The production application has no fixture switch,
// synthetic identity or DB dependency. Never execute the backend Program/.env.
internal static class BrowserHostProgram
{
    public static async Task Main()
    {
        var role = Environment.GetEnvironmentVariable("BROWSER_FIXTURE_ROLE") ?? ApplicationAccessRoles.PhysicalPersons;
        if (!ApplicationAccessRoles.All.Contains(role)) throw new InvalidOperationException("Unknown fixture role.");
        var user = new FixtureUser(role + "@fixture.test", role);
        var database = new FixtureDatabase();
        await using var backend = await CreateBackendAsync(user, database);
        await using var frontend = new BrowserApp(backend, user, database);
        var address = new Uri(Environment.GetEnvironmentVariable("BROWSER_FIXTURE_URL") ?? "http://127.0.0.1:5273");
        if (!address.IsLoopback || address.Scheme != "http") throw new InvalidOperationException("Browser fixtures are loopback-only.");
        WriteWorkbooks(FindRoot());
        frontend.UseKestrel(options => options.Listen(IPAddress.Loopback, address.Port));
        frontend.StartServer();
        await using (var smoke = frontend.Services.CreateAsyncScope())
        {
            var profile = await smoke.ServiceProvider.GetRequiredService<IProfileApi>().GetAsync();
            if (!profile.Permissions.Contains("FL_VIEW")) throw new InvalidOperationException("Fixture profile grants did not reach the real client.");
        }
        Console.WriteLine($"Browser fixture ready on {address}; role={role}; isolated InMemory store; no external services.");
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) => { args.Cancel = true; cancellation.Cancel(); };
        try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellation.Token); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }

    private static async Task<WebApplication> CreateBackendAsync(FixtureUser user, FixtureDatabase database)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing", ApplicationName = typeof(RelatedPersonController).Assembly.FullName });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(RelatedPersonController).Assembly);
        builder.Services.AddSingleton(user);
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database.Name));
        builder.Services.AddAuthentication("BrowserFixture").AddScheme<AuthenticationSchemeOptions, FixtureIdentity>("BrowserFixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        AuthenticationServiceExtensions.AddApplicationAuthorization(builder.Services);
        builder.Services.AddScoped<IRelatedPersonService, RelatedPersonService>();
        builder.Services.AddScoped<ICodeListService, CodeListService>();
        builder.Services.AddScoped<IPeriodLockRepository, PeriodLockRepository>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddSingleton(Mock.Of<IEmailService>());
        builder.Services.AddSingleton(Mock.Of<ILegalEntityService>());
        builder.Services.Configure<EmailSettings>(_ => { });
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            database.LastCulture = context.Request.Headers.AcceptLanguage.ToString();
            if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/related-persons")) database.MutationAttempts++;
            if (database.Unavailable || (database.CodebooksUnavailable && context.Request.Path.StartsWithSegments("/api/code-lists/dropdown")))
            { context.Response.StatusCode = 503; await context.Response.WriteAsync("private-fixture-detail-not-for-ui"); return; }
            await next(context);
        });
        app.UseAuthentication(); app.UseAuthorization(); app.UseMiddleware<PeriodLockMiddleware>(); app.MapControllers();
        app.MapGet("/api/auth/profile", (ClaimsPrincipal principal) => Results.Ok(new
        {
            userId = "fixture-user", fullName = "Browser Fixture", username = user.Email, email = user.Email,
            roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray(),
            permissions = principal.FindAll("permission").Select(claim => claim.Value).ToArray()
        })).RequireAuthorization();
        await app.StartAsync();
        await SeedAsync(app, user);
        return app;
    }

    private static async Task SeedAsync(WebApplication app, FixtureUser user)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var dbUser = new AppUser { Username = user.Email, Email = user.Email, KeycloakId = "fixture-user" };
        db.AppUsers.Add(dbUser);
        db.UserRoles.Add(new UserRole { UserId = dbUser.Id, RoleId = (await db.Roles.SingleAsync(item => item.Name == user.Role)).Id });
        db.CodeLists.AddRange(
            new CodeList { Kategorija = "OsnovPovezanosti", Kod = "ZOB-2-V-5", Naziv = "Uprava / Management", Opis = "Fixture relationship description", KreiraoKorisnik = "fixture" },
            new CodeList { Kategorija = "OsnovPosebnogOdnosa", Kod = "UPRAVA", Naziv = "Uprava / Management", KreiraoKorisnik = "fixture" },
            new CodeList { Kategorija = "OsnovPosebnogOdnosa", Kod = "UZA_PORODICA", Naziv = "Uža porodica / Immediate family", KreiraoKorisnik = "fixture" });
        if (user.Role == ApplicationAccessRoles.Guest)
            db.RelatedPersons.Add(new RBBH.ConnectedParties.DL.Entities.RelatedPersons.RelatedPerson
            {
                FirstName = "ReadOnly", LastName = "Fixture", Residency = RBBH.ConnectedParties.DL.Entities.RelatedPersons.ResidencyType.NonResident,
                PassportNumber = "READ12345", FBAId = "998877", RelationBasis = "ZOB-2-V-5", RelationDescription = "Fixture relationship",
                SpecialRelationBasis = "UPRAVA", IsIdentifiedStaff = true, IsActive = true, CreatedBy = "fixture"
            });
        await db.SaveChangesAsync();
    }

    internal static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.Migration.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Migration solution not found.");
    }

    private static void WriteWorkbooks(string root)
    {
        var directory = Path.Combine(root, "artifacts", "migration-browser-fixtures");
        Directory.CreateDirectory(directory);
        using var valid = new XLWorkbook();
        var sheet = valid.AddWorksheet("Physical persons");
        string[] headers = ["First name", "Last name", "Residency", "National ID", "Passport number", "FBA ID", "Relation basis", "Special relationship basis", "Date from", "Date to", "No family members", "Related to the Bank", "Special relationship with the Bank", "Special contract", "Malus & Clawback"];
        string[] values = ["Import", "Fixture", "NonResident", "", "IMP9876", "987654", "ZOB-2-V-5", "UPRAVA", "2026-01-01", "2026-12-31", "No", "Yes", "No", "No", "No"];
        for (var index = 0; index < headers.Length; index++) { sheet.Cell(1, index + 1).Value = headers[index]; sheet.Cell(2, index + 1).Value = values[index]; }
        valid.SaveAs(Path.Combine(directory, "valid-physical.xlsx"));
        using var invalid = new XLWorkbook();
        invalid.AddWorksheet("Invalid").Cell(1, 1).Value = "Wrong header";
        invalid.SaveAs(Path.Combine(directory, "invalid-physical.xlsx"));
    }

    private sealed class BrowserApp(WebApplication backend, FixtureUser user, FixtureDatabase database) : WebApplicationFactory<RBBH.RelatedPartiesRegister.Web.Components.App>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(Path.Combine(FindRoot(), "src", "Web.Blazor"));
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "", ["Authentication:ClientId"] = "", ["Authentication:ClientSecret"] = "", ["Apis:BaseUrl"] = "http://backend.fixture/"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options => options.DetailedErrors = true);
                services.AddSingleton<IStartupFilter>(new FixtureReset(backend, user, database));
                services.AddSingleton(user);
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "BrowserFixture"; options.DefaultAuthenticateScheme = "BrowserFixture";
                }).AddScheme<AuthenticationSchemeOptions, FixtureIdentity>("BrowserFixture", _ => { });
                // Configure real production typed clients explicitly: minimal-host
                // config callbacks may run after Program has inspected empty URLs.
                services.RemoveAll<IProfileApi>(); services.RemoveAll<IRelatedPersonsApi>();
                services.AddRbiApiClient<IProfileApi, ProfileApi>("BrowserProfile", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IRelatedPersonsApi, RelatedPersonsApi>("BrowserPersons", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
            });
        }
    }

    private sealed record FixtureUser(string Email, string Role);
    private sealed class FixtureDatabase
    {
        public string Name { get; set; } = "browser-" + Guid.NewGuid();
        public bool Unavailable { get; set; }
        public bool CodebooksUnavailable { get; set; }
        public string LastCulture { get; set; } = "";
        public int MutationAttempts { get; set; }
    }
    private sealed class FixtureReset(WebApplication backend, FixtureUser user, FixtureDatabase database) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => application =>
        {
            application.Use(async (HttpContext context, RequestDelegate nextMiddleware) =>
            {
                if (context.Request.Path.StartsWithSegments("/__fixtures"))
                {
                    if (context.Request.Headers["X-Fixture-Control"] != "true")
                    { context.Response.StatusCode = 404; return; }
                    if (context.Request.Path == "/__fixtures/counts" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(new { persons = await db.RelatedPersons.CountAsync(), audit = await db.AuditLogs.CountAsync(), mutations = database.MutationAttempts });
                        return;
                    }
                    if (context.Request.Path == "/__fixtures/culture" && HttpMethods.IsGet(context.Request.Method))
                    { await context.Response.WriteAsJsonAsync(new { language = database.LastCulture }); return; }
                    if (!HttpMethods.IsPost(context.Request.Method)) { context.Response.StatusCode = 404; return; }
                    switch (context.Request.Path.Value)
                    {
                        case "/__fixtures/reset":
                            // Fresh unique InMemory store; never delete an existing database.
                            database.Name = "browser-" + Guid.NewGuid(); database.Unavailable = database.CodebooksUnavailable = false;
                            database.MutationAttempts = 0;
                            await SeedAsync(backend, user); break;
                        case "/__fixtures/unavailable": database.Unavailable = true; break;
                        case "/__fixtures/available": database.Unavailable = false; break;
                        case "/__fixtures/codebooks-unavailable": database.CodebooksUnavailable = true; break;
                        case "/__fixtures/lock":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                db.PeriodLocks.Add(new PeriodLock { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month, IsLocked = true, LockedBy = "fixture", LockedAt = DateTime.UtcNow });
                                await db.SaveChangesAsync();
                            }
                            break;
                        default: context.Response.StatusCode = 404; return;
                    }
                    context.Response.StatusCode = 204;
                    return;
                }
                await nextMiddleware(context);
            });
            next(application);
        };
    }
    private sealed class FixtureIdentity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, FixtureUser user)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Name, user.Email)], "BrowserFixture")), "BrowserFixture")));
    }
}
