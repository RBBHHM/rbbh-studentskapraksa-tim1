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
using RBBH.ConnectedParties.DL.Entities.Notifications;
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
internal static partial class BrowserHostProgram
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
        builder.Configuration["Email:AdminEmail"] = "admin@fixture.test";
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
        // Backend services added by the finalization; the real controllers resolve them.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<PeriodWorkflow>();
        builder.Services.AddScoped<RoleRequestService>();
        builder.Services.AddScoped<RoleCatalogService>();
        builder.Services.AddScoped<NotificationDispatcher>();
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient<GitHubReleaseNotesService>()
            .ConfigurePrimaryHttpMessageHandler(() => new FixtureGitHubReleasesHandler());
        builder.Services.AddSingleton<RuntimeLocalizationCache>();
        builder.Services.AddOptions<RuntimeLocalizationOptions>().Bind(builder.Configuration.GetSection("Localization"));
        builder.Services.AddScoped<RuntimeLocalizationCatalog>();
        builder.Services.AddScoped<RBBH.ConnectedParties.Localization.BackendLocalizationFilter>();
        builder.Services.AddOptions<BankReferenceOptions>().Bind(builder.Configuration.GetSection("BankReference"));
        builder.Services.AddSingleton<IBankReferenceSource>(new FixtureBankSource(database)); // test-only stand-in; the real source needs bank access
        builder.Services.AddOptions<FinanceMonthlyOptions>().Bind(builder.Configuration.GetSection("FinanceMonthly"));
        builder.Services.AddScoped<IFinanceMonthlySource, SqlFinanceMonthlySource>();
        builder.Configuration["KeycloakSettings:Enabled"] = "false";
        builder.Services.AddHttpClient<KeycloakAdminService>().ConfigurePrimaryHttpMessageHandler(() => new NoIdentityNetwork());
        builder.Services.AddScoped(_ => database.MailStore);
        builder.Configuration["Email:Provider"] = "demo";
        builder.Services.Configure<EmailSettings>(_ => { });
        var app = builder.Build();
        app.UseExceptionHandler();
        // Fault injection only: the backend now commits business state, audit and notices atomically, so the
        // 'persisted but unconfirmed' case is simulated by losing the success response after the commit.
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "";
            var lose = HttpMethods.IsPost(context.Request.Method) && ((database.FailGenerationAudit && path.StartsWith("/api/reports/", StringComparison.OrdinalIgnoreCase)) ||
                (database.FailPeriodAudit && path.StartsWith("/api/period-lock/", StringComparison.OrdinalIgnoreCase) && (path.EndsWith("/respond", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/request-unlock", StringComparison.OrdinalIgnoreCase))));
            if (!lose) { await next(); return; }
            var original = context.Response.Body; using var buffer = new MemoryStream(); context.Response.Body = buffer;
            try { await next(); } finally { context.Response.Body = original; }
            context.Response.StatusCode = StatusCodes.Status500InternalServerError; context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync("{\"status\":500}");
        });
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
            if (database.Unavailable || (database.PeriodUnavailable && context.Request.Path == "/api/period-lock/current") || (database.CodebooksUnavailable && context.Request.Path.StartsWithSegments("/api/code-lists/dropdown")) ||
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
            applicationUserId = principal.FindFirst("rbbh.app_user_id")?.Value,
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
        var dbUser = new AppUser { Username = user.Email, Email = user.Email, KeycloakSubjectConfirmed = true, KeycloakId = "fixture-user" };
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
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.slnx"))) directory = directory.Parent;
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
            builder.UseContentRoot(Path.Combine(FindRoot(), "Frontend"));
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
                services.RemoveAll<IRoleCatalogApi>();
                services.RemoveAll<IReleaseNotesApi>();
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
                services.AddRbiApiClient<ILocalizationApi, LocalizationApi>("BrowserLocalization", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IRoleRequestsApi, RoleRequestsApi>("BrowserRoleRequests", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IRoleCatalogApi, RoleCatalogApi>("BrowserRoleCatalog", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
                services.AddRbiApiClient<IReleaseNotesApi, ReleaseNotesApi>("BrowserReleaseNotes", options => options.BaseUrl = new("http://backend.fixture/"))
                    .AddHttpMessageHandler<ApplicationCultureHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => backend.GetTestServer().CreateHandler());
            });
        }
    }

    /// <summary>Test-only bank reference source. Disabled it behaves like an unconfigured production source (503); enabled it serves two synthetic groups.</summary>
    private sealed class FixtureBankSource(FixtureDatabase database) : IBankReferenceSource
    {
        private static readonly BankGroup[] Known = [new("1001", "Fixture Group A"), new("1002", "Fixture Group B")];
        private static readonly BankCustomer[] KnownCustomers = [new("1234567890125", "789456123", "Fixture Bank Customer")];
        public bool IsConfigured => database.BankSourceEnabled;
        private void Require() { if (!database.BankSourceEnabled) throw new RBBH.ConnectedParties.Exceptions.BusinessRuleException(503, "bank_reference.unavailable"); }
        public Task<ReferencePage<BankCustomer>> Customers(string? search, int page, int pageSize, CancellationToken ct)
        {
            Require();
            var matches = KnownCustomers.Where(item => string.IsNullOrWhiteSpace(search) ||
                item.TaxNumber.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                item.Matbroj.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new ReferencePage<BankCustomer>(matches.Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).ToArray(), matches.Length, page, pageSize));
        }
        public Task<ReferencePage<BankGroup>> Groups(string? search, int page, int pageSize, CancellationToken ct)
        {
            Require();
            var items = Known.Where(item => string.IsNullOrWhiteSpace(search) || item.Number.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) || item.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new ReferencePage<BankGroup>(items, items.Length, page, pageSize));
        }
        public Task<BankGroup?> Group(string number, CancellationToken ct) { Require(); return Task.FromResult<BankGroup?>(Known.FirstOrDefault(item => item.Number == number)); }
    }
    private sealed class FixtureGitHubReleasesHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri?.Host != "api.github.com")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
            const string json = """
                [{"tag_name":"uat-2.0.0","name":"UAT test","body":"UAT release details","published_at":"2026-11-11T10:00:00Z","prerelease":true,"draft":false,"html_url":"https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/releases/tag/uat-2.0.0"},
                 {"tag_name":"v1.9.0","name":"PROD test","body":"Production notes","published_at":"2026-11-01T10:00:00Z","prerelease":false,"draft":false,"html_url":"https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/releases/tag/v1.9.0"}]
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
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
        public bool PeriodUnavailable { get; set; }
        public bool GeneratedDownloadUnavailable { get; set; }
        public bool FailGenerationAudit { get; set; }
        public bool BankSourceEnabled { get; set; }
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
    // HTTP fixture controls live in BrowserFixtureControl.cs (test-only).
    private sealed class FixtureIdentity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, FixtureUser user, FixtureDatabase database)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "fixture-user"), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Name, user.Email), new("email_verified", "true"), new("preferred_username", user.Email) };
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
