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
using RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling;
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
        builder.Services.AddExceptionHandlingExtension(builder.Environment);
        builder.Services.AddSingleton(user);
        builder.Services.AddSingleton(database);
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database.Name));
        builder.Services.AddAuthentication("BrowserFixture").AddScheme<AuthenticationSchemeOptions, FixtureIdentity>("BrowserFixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        AuthenticationServiceExtensions.AddApplicationAuthorization(builder.Services);
        builder.Services.AddScoped<IRelatedPersonService, RelatedPersonService>();
        builder.Services.AddScoped<ICodeListService, CodeListService>();
        builder.Services.AddScoped<IPeriodLockRepository, PeriodLockRepository>();
        builder.Services.AddScoped<IUnlockRequestRepository, UnlockRequestRepository>();
        builder.Services.AddScoped<IPeriodLockService, PeriodLockService>();
        builder.Services.AddScoped<AuditService>();
        builder.Services.AddScoped<IAuditService>(services => new AuditFaults(services.GetRequiredService<AuditService>(), database));
        builder.Services.AddSingleton(Mock.Of<IEmailService>());
        builder.Services.AddScoped<ILegalEntityService, LegalEntityService>();
        builder.Services.AddScoped<ILimitService, LimitService>();
        builder.Services.AddScoped<ICapitalService, CapitalService>();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddScoped<IAppUserService, AppUserService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Configuration["KeycloakSettings:Enabled"] = "false";
        builder.Services.AddHttpClient<KeycloakAdminService>().ConfigurePrimaryHttpMessageHandler(() => new NoIdentityNetwork());
        builder.Services.AddScoped(_ => database.MailStore);
        builder.Configuration["Email:Provider"] = "demo";
        builder.Services.Configure<EmailSettings>(_ => { });
        var app = builder.Build();
        app.UseExceptionHandler();
        app.Use(async (context, next) =>
        {
            if (database.DuplicateBarrier is { } barrier && context.Request.Path == "/api/related-persons/check-duplicate" && context.Request.Query.ContainsKey("fbaId"))
                await barrier.Task.WaitAsync(context.RequestAborted);
            database.LastCulture = context.Request.Headers.AcceptLanguage.ToString();
            if (context.Request.Path == "/api/audit-logs") database.AuditRequests++;
            if ((HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method)) &&
                (context.Request.Path.StartsWithSegments("/api/related-persons") || context.Request.Path.StartsWithSegments("/api/legal-entities") || context.Request.Path.StartsWithSegments("/api/limiti") || context.Request.Path.StartsWithSegments("/api/capital") || context.Request.Path.StartsWithSegments("/api/reports") || context.Request.Path.StartsWithSegments("/api/users") || context.Request.Path.StartsWithSegments("/api/period-lock"))) database.MutationAttempts++;
            if ((HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method)) && context.Request.Path.StartsWithSegments("/api/code-lists")) database.MutationAttempts++;
            if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/reports")) database.ReportGenerationAttempts++;
            if (database.Unavailable || (database.CodebooksUnavailable && context.Request.Path.StartsWithSegments("/api/code-lists/dropdown")) ||
                (database.ProfileUnavailable && context.Request.Path == "/api/auth/profile") ||
                (database.GeneratedDownloadUnavailable && context.Request.Path.StartsWithSegments("/api/reports/export/generated")))
            { context.Response.StatusCode = 503; await context.Response.WriteAsync("private-fixture-detail-not-for-ui"); return; }
            await next(context);
        });
        app.UseAuthentication(); app.UseAuthorization(); app.UseMiddleware<PeriodLockMiddleware>(); app.MapControllers();
        app.MapGet("/api/auth/profile", (ClaimsPrincipal principal) => Results.Ok(new
        {
            userId = "fixture-user",
            fullName = "Browser Fixture",
            username = user.Email,
            email = user.Email,
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
                FirstName = "ReadOnly",
                LastName = "Fixture",
                Residency = RBBH.ConnectedParties.DL.Entities.RelatedPersons.ResidencyType.NonResident,
                PassportNumber = "READ12345",
                FBAId = "998877",
                RelationBasis = "ZOB-2-V-5",
                RelationDescription = "Fixture relationship",
                SpecialRelationBasis = "UPRAVA",
                IsIdentifiedStaff = true,
                IsActive = true,
                CreatedBy = "fixture"
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
        using var legal = new XLWorkbook();
        var legalSheet = legal.AddWorksheet("Legal entities");
        string[] legalHeaders = ["Name", "Type", "Tax number", "FBA ID", "Registration number", "Connection basis", "Connection description", "Related to the Bank", "Date from", "Date to"];
        string[] legalValues = ["Imported Company", "Non-resident", "", "777888", "55555", "ZOB-2-V-5", "Fixture relationship", "Yes", "2026-01-01", "2026-12-31"];
        for (var index = 0; index < legalHeaders.Length; index++) { legalSheet.Cell(1, index + 1).Value = legalHeaders[index]; legalSheet.Cell(2, index + 1).Value = legalValues[index]; }
        legal.SaveAs(Path.Combine(directory, "valid-legal.xlsx"));
        using var invalid = new XLWorkbook();
        invalid.AddWorksheet("Invalid").Cell(1, 1).Value = "Wrong header";
        invalid.SaveAs(Path.Combine(directory, "invalid-physical.xlsx"));
        using var codes = new XLWorkbook();
        var codesSheet = codes.AddWorksheet("Values");
        string[] codeRows = ["Value", "Existing", "New imported", "New imported", new string('x', 201), "A B"];
        for (var index = 0; index < codeRows.Length; index++) codesSheet.Cell(index + 1, 1).Value = codeRows[index];
        codes.SaveAs(Path.Combine(directory, "code-values.xlsx"));
        using var invalidCodes = new XLWorkbook();
        var invalidCodesSheet = invalidCodes.AddWorksheet("Invalid"); invalidCodesSheet.Cell(1, 1).Value = "Wrong header"; invalidCodesSheet.Cell(2, 1).Value = "Must not persist";
        invalidCodes.SaveAs(Path.Combine(directory, "invalid-codes.xlsx"));
    }

    private sealed class BrowserApp(WebApplication backend, FixtureUser user, FixtureDatabase database) : WebApplicationFactory<RBBH.RelatedPartiesRegister.Web.Components.App>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(Path.Combine(FindRoot(), "src", "Web.Blazor"));
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "",
                ["Authentication:ClientId"] = "",
                ["Authentication:ClientSecret"] = "",
                ["Apis:BaseUrl"] = "http://backend.fixture/"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options => options.DetailedErrors = true);
                services.AddSingleton<IStartupFilter>(new FixtureReset(backend, user, database));
                services.AddSingleton(user);
                services.AddSingleton(database);
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "BrowserFixture"; options.DefaultAuthenticateScheme = "BrowserFixture";
                }).AddScheme<AuthenticationSchemeOptions, FixtureIdentity>("BrowserFixture", _ => { });
                // Configure real production typed clients explicitly: minimal-host
                // config callbacks may run after Program has inspected empty URLs.
                services.RemoveAll<IProfileApi>(); services.RemoveAll<IRelatedPersonsApi>(); services.RemoveAll<ILegalEntitiesApi>();
                services.RemoveAll<ILimitsApi>();
                services.RemoveAll<ICapitalApi>();
                services.RemoveAll<IReportsApi>();
                services.RemoveAll<INotificationsApi>();
                services.RemoveAll<IAuditApi>();
                services.RemoveAll<IUsersApi>();
                services.RemoveAll<IPeriodApi>();
                services.RemoveAll<ICodeListsApi>();
                services.AddRbiApiClient<IProfileApi, ProfileApi>("BrowserProfile", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IRelatedPersonsApi, RelatedPersonsApi>("BrowserPersons", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<ILegalEntitiesApi, LegalEntitiesApi>("BrowserLegal", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<ILimitsApi, LimitsApi>("BrowserLimits", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<ICapitalApi, CapitalApi>("BrowserCapital", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IReportsApi, ReportsApi>("BrowserReports", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<INotificationsApi, NotificationsApi>("BrowserNotifications", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IAuditApi, AuditApi>("BrowserAudit", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IUsersApi, UsersApi>("BrowserUsers", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IPeriodApi, PeriodApi>("BrowserPeriod", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<ICodeListsApi, CodeListsApi>("BrowserCodeLists", options => options.BaseUrl = new("http://backend.fixture/"))
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
        public bool ProfileUnavailable { get; set; }
        public bool GeneratedDownloadUnavailable { get; set; }
        public bool FailGenerationAudit { get; set; }
        public bool FailPeriodAudit { get; set; }
        public TaskCompletionSource? DuplicateBarrier { get; set; }
        public int ReportGenerationAttempts { get; set; }
        public EmailLogStore MailStore { get; set; } = new();
        public int AuditRequests { get; set; }
        public bool ForgeAdmin { get; set; }
    }
    // Fault injection only: preserve real auditing except the explicit failure
    // after ReportService has already persisted a snapshot, before HTTP201.
    private sealed class AuditFaults(AuditService inner, FixtureDatabase database) : IAuditService
    {
        public Task LogAsync(AuditEntry entry) => (database.FailGenerationAudit && entry.Action == "GENERATE") || (database.FailPeriodAudit && entry.TableName == "PeriodLock" && entry.Action == "UNLOCK_REQUEST")
            ? Task.FromException(new InvalidOperationException("private-fixture-audit-failure")) : inner.LogAsync(entry);
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
                    if (context.Request.Path == "/__fixtures/users-state" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(new
                        {
                            users = await db.AppUsers.Select(item => new { item.Id, item.Username, item.FirstName, item.LastName, item.Email, item.IsActive }).ToArrayAsync(),
                            roles = await db.Roles.Select(item => new { item.Id, item.Name }).ToArrayAsync(),
                            audit = await db.AuditLogs.Select(item => new { item.Action, item.RecordId, item.Username }).ToArrayAsync()
                        }); return;
                    }
                    if (context.Request.Path == "/__fixtures/period-state" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(new
                        {
                            requests = await db.UnlockRequests.OrderBy(item => item.CreatedAt).Select(item => new { item.Id, item.Status, item.Reason, item.RequestedBy, item.Year, item.Month }).ToArrayAsync(),
                            locks = await db.PeriodLocks.Select(item => new { item.Year, item.Month, item.IsLocked }).ToArrayAsync(),
                            audit = await db.AuditLogs.OrderBy(item => item.Timestamp).Select(item => new { item.Action, item.RecordId, item.Username }).ToArrayAsync()
                        }); return;
                    }
                    if (context.Request.Path == "/__fixtures/codes-state" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(new
                        {
                            values = await db.CodeLists.IgnoreQueryFilters().OrderBy(item => item.ID).Select(item => new { item.ID, item.Kategorija, item.Kod, item.Naziv, item.Opis, item.RedoslijedPrikaza, item.Aktivan, item.IzmijenioKorisnik }).ToArrayAsync(),
                            definitions = await db.CodeListDefinitions.Select(item => new { item.Name, item.Description, item.IsActive }).ToArrayAsync(),
                            audit = await db.AuditLogs.OrderBy(item => item.Timestamp).Select(item => new { item.Action, item.RecordId, item.Username, item.NewValues }).ToArrayAsync()
                        }); return;
                    }
                    if (context.Request.Path == "/__fixtures/codes-denied-probes" && HttpMethods.IsPost(context.Request.Method))
                    {
                        using var client = backend.GetTestClient();
                        using var read = await client.GetAsync("/api/code-lists");
                        using var category = await client.PostAsJsonAsync("/api/code-lists/categories", new { name = "Forbidden" });
                        using var create = await client.PostAsJsonAsync("/api/code-lists", new { kategorija = "MigrationCodes", kod = "FORBIDDEN", naziv = "Forbidden" });
                        using var update = await client.PutAsJsonAsync("/api/code-lists/1", new { naziv = "Forbidden", aktivan = false });
                        using var delete = await client.DeleteAsync("/api/code-lists/1");
                        using var deleteCategory = await client.DeleteAsync("/api/code-lists/categories/OsnovPovezanosti");
                        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent([1, 2, 3]), "file", "invalid.xlsx");
                        using var import = await client.PostAsync("/api/code-lists/MigrationCodes/import?dryRun=false", form);
                        await context.Response.WriteAsJsonAsync(new { read = (int)read.StatusCode, writes = new[] { (int)category.StatusCode, (int)create.StatusCode, (int)update.StatusCode, (int)delete.StatusCode, (int)deleteCategory.StatusCode, (int)import.StatusCode } }); return;
                    }
                    if (context.Request.Path == "/__fixtures/authorization-matrix" && HttpMethods.IsPost(context.Request.Method))
                    {
                        using var client = backend.GetTestClient();
                        var results = new Dictionary<string, int>();
                        // Invalid business input must reach validation only for
                        // authorized roles. It must never create a real record.
                        foreach (var (key, path, body) in new (string, string, object)[]
                        {
                            ("physical", "/api/related-persons", new { }),
                            ("legal", "/api/legal-entities", new { }),
                            ("limits", "/api/limiti", new { }),
                            ("users", "/api/users", new { }),
                            ("codes", "/api/code-lists", new { }),
                            ("period", $"/api/period-lock/unlock-requests/{Guid.Empty}/respond", new { message = "123456789" }),
                            ("generate", "/api/reports/monthly/1/0", new { })
                        })
                        {
                            using var response = await client.PostAsJsonAsync(path, body);
                            results[key] = (int)response.StatusCode;
                        }
                        using var capital = await client.PutAsJsonAsync("/api/capital", new { regulatoryCapital = -1, coreCapital = -1, supplementaryCapital = -1, capitalDate = "2026-01-01" });
                        results["capital"] = (int)capital.StatusCode;
                        foreach (var (key, path) in new[] { ("reportRead", "/api/reports/daily"), ("periodRead", "/api/period-lock/current"), ("codesRead", "/api/code-lists"), ("usersRead", "/api/users"), ("auditRead", "/api/audit-logs") })
                        {
                            using var response = await client.GetAsync(path); results[key] = (int)response.StatusCode;
                        }
                        await context.Response.WriteAsJsonAsync(results); return;
                    }
                    if (context.Request.Path == "/__fixtures/period-response" && HttpMethods.IsPost(context.Request.Method))
                    {
                        var input = await context.Request.ReadFromJsonAsync<PeriodResponseFixture>();
                        using var client = backend.GetTestClient();
                        using var response = await client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{input!.Id}/respond", new { message = input.Message });
                        await context.Response.WriteAsJsonAsync(new { status = (int)response.StatusCode }); return;
                    }
                    if (context.Request.Path == "/__fixtures/users-status" && HttpMethods.IsGet(context.Request.Method))
                    {
                        using var client = backend.GetTestClient();
                        using var response = await client.GetAsync("/api/users");
                        await context.Response.WriteAsJsonAsync(new { status = (int)response.StatusCode }); return;
                    }
                    if (context.Request.Path == "/__fixtures/users-mutation" && HttpMethods.IsPost(context.Request.Method))
                    {
                        var input = await context.Request.ReadFromJsonAsync<UsersMutationFixture>();
                        using var client = backend.GetTestClient();
                        using var response = input!.Action switch
                        {
                            "deactivate" => await client.DeleteAsync($"/api/users/{input.Id}"),
                            "delete" => await client.DeleteAsync($"/api/users/{input.Id}/permanent"),
                            "roles" => await client.PutAsJsonAsync($"/api/users/{input.Id}/role", new { roleIds = input.RoleIds ?? [] }),
                            _ => throw new InvalidOperationException("Unknown test operation.")
                        };
                        await context.Response.WriteAsJsonAsync(new { status = (int)response.StatusCode }); return;
                    }
                    if (context.Request.Path == "/__fixtures/audit-requests" && HttpMethods.IsGet(context.Request.Method))
                    { await context.Response.WriteAsJsonAsync(new { count = database.AuditRequests }); return; }
                    if (context.Request.Path == "/__fixtures/audit-status" && HttpMethods.IsGet(context.Request.Method))
                    {
                        using var client = backend.GetTestClient();
                        using var response = await client.GetAsync("/api/audit-logs");
                        await context.Response.WriteAsJsonAsync(new { status = (int)response.StatusCode }); return;
                    }
                    if (context.Request.Path == "/__fixtures/front-identity" && HttpMethods.IsGet(context.Request.Method))
                    {
                        var identity = await context.AuthenticateAsync("BrowserFixture");
                        await context.Response.WriteAsJsonAsync(new { providerAdmin = identity.Principal?.IsInRole("admin") == true }); return;
                    }
                    if (context.Request.Path == "/__fixtures/legal-record" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(await db.LegalEntities.IgnoreQueryFilters().OrderBy(item => item.CreatedAt).Select(item => new { item.Name, item.Matbroj, item.MaticniBroj, item.Status, item.IsActive, item.VerifiedBy, item.ConnectedWithBank, item.DateTo }).ToArrayAsync());
                        return;
                    }
                    if (context.Request.Path == "/__fixtures/limits" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(await db.Limiti.AsNoTracking().Select(item => new { item.Id, item.LegalEntityId, item.TipLimita, item.OdobreniLimit, item.MaksimalnaOcekivanaUtilizacija, item.Komentar }).ToArrayAsync());
                        return;
                    }
                    if (context.Request.Path == "/__fixtures/capital" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(await db.Capitals.AsNoTracking().Select(item => new { item.Id, item.RegulatoryCapital, item.CoreCapital, item.SupplementaryCapital, item.CapitalDate }).ToArrayAsync());
                        return;
                    }
                    if (context.Request.Path == "/__fixtures/reports" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        await context.Response.WriteAsJsonAsync(new
                        {
                            attempts = database.ReportGenerationAttempts,
                            records = await db.Reports.AsNoTracking().OrderBy(item => item.CreatedAt).Select(item => new { item.Id, item.ReportType, item.ReportDate, item.CreatedBy, item.TotalClients, item.TotalExposure, item.ClientsWithBreachedLimit, snapshotLength = item.DataSnapshot == null ? 0 : item.DataSnapshot.Length }).ToArrayAsync()
                        });
                        return;
                    }
                    if (!HttpMethods.IsPost(context.Request.Method)) { context.Response.StatusCode = 404; return; }
                    switch (context.Request.Path.Value)
                    {
                        case "/__fixtures/reset":
                            // Fresh unique InMemory store; never delete an existing database.
                            database.Name = "browser-" + Guid.NewGuid(); database.Unavailable = database.CodebooksUnavailable = false;
                            database.ProfileUnavailable = database.GeneratedDownloadUnavailable = database.FailGenerationAudit = false;
                            database.FailPeriodAudit = false;
                            database.DuplicateBarrier?.TrySetResult(); database.DuplicateBarrier = null;
                            database.ReportGenerationAttempts = 0;
                            database.MailStore = new(); backend.Configuration["Email:Provider"] = "demo";
                            database.AuditRequests = 0; database.ForgeAdmin = false;
                            database.MutationAttempts = 0;
                            await SeedAsync(backend, user); break;
                        case "/__fixtures/unavailable": database.Unavailable = true; break;
                        case "/__fixtures/period-audit-fails": database.FailPeriodAudit = true; break;
                        case "/__fixtures/period-audit-restored": database.FailPeriodAudit = false; break;
                        case "/__fixtures/duplicates-hold": database.DuplicateBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously); break;
                        case "/__fixtures/duplicates-release": database.DuplicateBarrier?.TrySetResult(); break;
                        case "/__fixtures/users-reference":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                foreach (var roleName in new[] { ApplicationAccessRoles.Administrator, ApplicationAccessRoles.PhysicalPersons })
                                {
                                    var other = new AppUser { Email = $"other-{roleName}@fixture.test", Username = $"other-{roleName}", KeycloakId = Guid.NewGuid().ToString(), FirstName = "Fixture", LastName = roleName };
                                    db.AppUsers.Add(other);
                                    db.UserRoles.Add(new UserRole { UserId = other.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == roleName)).Id });
                                }
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/codes-reference":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                db.CodeListDefinitions.Add(new CodeListDefinition { Name = "MigrationCodes", Description = "Fixture purpose", CreatedBy = "fixture" });
                                db.CodeLists.AddRange(new CodeList { Kategorija = "MigrationCodes", Kod = "EXISTING", Naziv = "Existing", KreiraoKorisnik = "fixture" }, new CodeList { Kategorija = "MigrationCodes", Kod = "A_B", Naziv = "Different existing name", KreiraoKorisnik = "fixture" });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/period-reference":
                        case "/__fixtures/period-pages":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var now = DateTime.UtcNow;
                                var count = context.Request.Path == "/__fixtures/period-pages" ? 101 : 1;
                                for (var index = 0; index < count; index++)
                                    db.UnlockRequests.Add(new UnlockRequest { RequestedBy = $"requester{index:D3}", RequestedByEmail = "fixture@example.test", Year = now.Year, Month = now.Month, Reason = "Fixture request for clarification", Status = count == 1 ? "NEEDS_INFO" : "PENDING", CreatedBy = "fixture", CreatedAt = now.AddSeconds(-index) });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/users-pages":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                for (var index = 0; index < 22; index++)
                                    db.AppUsers.Add(new AppUser { Email = $"page{index}@fixture.test", Username = $"page{index:D2}", FirstName = "Page", LastName = $"Fixture{index:D2}", KeycloakId = Guid.NewGuid().ToString() });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/available": database.Unavailable = false; break;
                        case "/__fixtures/codebooks-unavailable": database.CodebooksUnavailable = true; break;
                        case "/__fixtures/profile-unavailable": database.ProfileUnavailable = true; break;
                        case "/__fixtures/profile-available": database.ProfileUnavailable = false; break;
                        case "/__fixtures/generated-downloads-unavailable": database.GeneratedDownloadUnavailable = true; break;
                        case "/__fixtures/generated-downloads-available": database.GeneratedDownloadUnavailable = false; break;
                        case "/__fixtures/generation-audit-fails": database.FailGenerationAudit = true; break;
                        case "/__fixtures/email-existing":
                            database.MailStore.Add(new EmailLogEntry
                            {
                                To = "fixture@example.test",
                                Subject = "Fixture notification",
                                Audience = "broadcast",
                                HtmlBody = "<img src=x onerror=\"window.__notificationExecuted=true\">body-search-needle"
                            });
                            break;
                        case "/__fixtures/email-smtp": backend.Configuration["Email:Provider"] = "smtp"; break;
                        case "/__fixtures/email-demo": backend.Configuration["Email:Provider"] = "demo"; break;
                        case "/__fixtures/forge-provider-admin": database.ForgeAdmin = true; break;
                        case "/__fixtures/audit-existing":
                            await using (var scope = backend.Services.CreateAsyncScope())
                                await scope.ServiceProvider.GetRequiredService<IAuditService>().LogAsync(new AuditEntry
                                {
                                    TableName = "RelatedPerson",
                                    RecordId = "fixture-record",
                                    Action = "INSERT",
                                    UserId = "fixture-user",
                                    Username = "fixture-auditor",
                                    IpAddress = "127.0.0.1",
                                    NewValues = System.Text.Json.JsonSerializer.Serialize(new { note = "<img src=x onerror=\"window.__auditExecuted=true\">audit-body-needle" })
                                });
                            break;
                        case "/__fixtures/audit-pages":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                for (var index = 0; index < 201; index++) db.AuditLogs.Add(new RBBH.ConnectedParties.DL.Entities.Audit.AuditLog
                                {
                                    TableName = "RelatedPerson",
                                    RecordId = $"audit-record-{index:D3}",
                                    Action = "INSERT",
                                    UserId = "fixture-user",
                                    Username = $"audit-page-{index:D3}",
                                    Timestamp = DateTime.UtcNow.AddMinutes(-index)
                                });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/report-reference":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var entity = await db.LegalEntities.FirstAsync();
                                db.ClientLimits.Add(new RBBH.ConnectedParties.DL.Entities.Report.ClientLimit
                                {
                                    LegalEntityId = entity.Id,
                                    CurrentExposure = 125.5m,
                                    ExposureLimit = 100m,
                                    IsLimitBreached = true,
                                    RegulatoryCapital = 1000m,
                                    CoreCapital = 500m,
                                    CreatedBy = "fixture"
                                });
                                // Separate current export contract uses Limiti, as in
                                // the unchanged backend. Do not replace its dataset.
                                db.Limiti.Add(new RBBH.ConnectedParties.DL.Entities.Limiti.Limit { LegalEntityId = entity.Id, TipLimita = "REG", OdobreniLimit = 100m, CreatedBy = "fixture" });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/report-existing":
                            await using (var scope = backend.Services.CreateAsyncScope())
                                await scope.ServiceProvider.GetRequiredService<IReportService>().GenerateDailyReportAsync("fixture-author");
                            break;
                        case "/__fixtures/reports-pages":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                for (var index = 0; index < 101; index++) db.Reports.Add(new RBBH.ConnectedParties.DL.Entities.Report.Report
                                {
                                    ReportType = "DAILY",
                                    ReportDate = DateTime.UtcNow.Date.AddDays(-index),
                                    CreatedAt = DateTime.UtcNow.AddMinutes(-index),
                                    CreatedBy = $"fixture-page-{index:D3}",
                                    DataSnapshot = "[]",
                                    IsActive = true
                                });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/limit-reference":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                db.CodeLists.Add(new CodeList { Kategorija = "VrstaLimita", Kod = "REG", Naziv = "Regulatorni / Regulatory", KreiraoKorisnik = "fixture" });
                                db.Capitals.Add(new RBBH.ConnectedParties.DL.Entities.Report.Capital { RegulatoryCapital = 1234567.89m, CoreCapital = 765432.10m, CapitalDate = new(2026, 1, 1), CreatedBy = "fixture" });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/limit-existing":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var entity = await db.LegalEntities.OrderBy(item => item.CreatedAt).FirstAsync();
                                db.Limiti.Add(new RBBH.ConnectedParties.DL.Entities.Limiti.Limit { LegalEntityId = entity.Id, TipLimita = "REG", OdobreniLimit = 125.50m, CreatedBy = "fixture", CreatedAt = DateTime.UtcNow });
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/limit-no-types":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                foreach (var type in await db.CodeLists.AsTracking().Where(item => item.Kategorija == "VrstaLimita").ToListAsync()) type.Aktivan = false;
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/legal-existing":
                        case "/__fixtures/legal-pages":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var count = context.Request.Path == "/__fixtures/legal-pages" ? 22 : 1;
                                for (var index = 0; index < count; index++) db.LegalEntities.Add(new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity
                                {
                                    Name = $"Existing Company {index:D2}",
                                    IsResident = false,
                                    FbaId = (100000 + index).ToString(System.Globalization.CultureInfo.InvariantCulture),
                                    MaticniBroj = "12345",
                                    Matbroj = "SERVER-ONLY-789",
                                    BasisOfConnection = "ZOB-2-V-5",
                                    ConnectionDescription = "Fixture relationship description",
                                    ConnectedWithBank = true,
                                    DateFrom = new(2026, 1, 1),
                                    Status = "Draft",
                                    CreatedBy = "fixture-author",
                                    IsActive = true,
                                    CreatedAt = new DateTime(2026, 1, 1).AddMinutes(index)
                                });
                                await db.SaveChangesAsync();
                            }
                            break;
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
    private sealed class FixtureIdentity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, FixtureUser user, FixtureDatabase database)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "fixture-user"), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Name, user.Email) };
            if (database.ForgeAdmin) claims.AddRange([new(ClaimTypes.Role, "admin"), new("permission", "ADMINISTRATION_MANAGE"), new("rct_done", "1")]);
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims, "BrowserFixture")), "BrowserFixture")));
        }
    }
    private sealed record UsersMutationFixture(Guid Id, string Action, Guid[]? RoleIds);
    private sealed record PeriodResponseFixture(Guid Id, string Message);
    private sealed class NoIdentityNetwork : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Browser fixtures never call the identity provider.");
    }
}
