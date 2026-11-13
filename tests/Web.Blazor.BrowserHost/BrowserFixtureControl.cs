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

// The HTTP fixture-control adapter is intentionally isolated from the native
// BrowserHost bootstrap. It only runs on loopback and accepts explicit fixture
// control headers; production never registers or links this test-only class.
internal static partial class BrowserHostProgram
{
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
                    if (context.Request.Path == "/__fixtures/role-requests-state" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await using var scope = backend.Services.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                        var requests = await db.RoleRequests.AsNoTracking().OrderBy(item => item.CreatedAt).Select(item => new { item.Username, item.Status, item.Purpose, roles = item.RequestedRoles.Select(r => r.Role.Name).ToArray() }).ToArrayAsync();
                        var requester = await db.AppUsers.AsNoTracking().Where(item => item.KeycloakId == "requester-subject").Select(item => item.Id).SingleOrDefaultAsync();
                        var granted = await db.UserRoles.AsNoTracking().Where(item => item.IsActive && item.UserId == requester).Select(item => item.Role.Name).ToArrayAsync();
                        var notices = await db.Notifications.AsNoTracking().Where(item => item.RecipientSubject == "requester-subject").Select(item => item.TitleKey).ToArrayAsync();
                        await context.Response.WriteAsJsonAsync(new { requests, granted, notices }); return;
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
                        await context.Response.WriteAsJsonAsync((await db.CapitalRevisions.AsNoTracking().OrderBy(item => item.Revision).ToArrayAsync()).Select(item => new { item.Id, item.Revision, item.RegulatoryCapital, item.CoreCapital, item.SupplementaryCapital, CapitalDate = item.EffectiveDate }).ToArray());
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
                            database.ProfileUnavailable = database.GeneratedDownloadUnavailable = database.FailGenerationAudit = database.BankSourceEnabled = false;
                            database.FailPeriodAudit = database.PeriodUnavailable = false;
                            database.DuplicateBarrier?.TrySetResult(); database.DuplicateBarrier = null;
                            database.ReportGenerationAttempts = 0;
                            database.MailStore = new(); backend.Configuration["Email:Provider"] = "demo";
                            database.AuditRequests = 0; database.ForgeAdmin = false;
                            database.MutationAttempts = 0;
                            await SeedAsync(backend, user); break;
                        case "/__fixtures/unavailable": database.Unavailable = true; break;
                        case "/__fixtures/period-unavailable": database.PeriodUnavailable = true; break;
                        case "/__fixtures/period-available": database.PeriodUnavailable = false; break;
                        case "/__fixtures/identity-unknown":
                        case "/__fixtures/identity-inactive":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var local = await db.AppUsers.SingleAsync(item => item.KeycloakId == "fixture-user");
                                if (context.Request.Path == "/__fixtures/identity-inactive") local.IsActive = false;
                                else { local.KeycloakId = "different-fixture-subject"; local.Email = "different-identity@example.test"; }
                                await db.SaveChangesAsync();
                            }
                            break;
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
                                    var other = new AppUser { Email = $"other-{roleName}@fixture.test", Username = $"other-{roleName}", KeycloakSubjectConfirmed = true, KeycloakId = Guid.NewGuid().ToString(), FirstName = "Fixture", LastName = roleName };
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
                                if (count == 1) db.Entry(db.UnlockRequests.Local.Single()).Property("RequestedBySubject").CurrentValue = "fixture-user";
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/role-requests-seed":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var admin = new AppUser { Username = "second-admin", Email = "second-admin@fixture.test", FirstName = "Second", LastName = "Admin", KeycloakSubjectConfirmed = true, KeycloakId = "second-admin-subject" };
                                db.AppUsers.Add(admin);
                                db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = (await db.Roles.SingleAsync(item => item.Name == "admin")).Id });
                                var fl = await db.Roles.SingleAsync(item => item.Name == "FL");
                                var request = new RBBH.ConnectedParties.DL.Entities.Authorization.RoleRequest { ClientRequestId = Guid.NewGuid(), RequesterSubject = "requester-subject", Username = "requester", FirstName = "Re", LastName = "Quester", Email = "requester@fixture.test", Purpose = "new", Note = "Need FL for onboarding" };
                                request.RequestedRoles = [new RBBH.ConnectedParties.DL.Entities.Authorization.RequestedRole { Role = fl, RoleId = fl.Id, RequestId = request.Id }];
                                db.RoleRequests.Add(request);
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/users-unconfirmed":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var user = new AppUser
                                {
                                    Email = "unconfirmed@fixture.test",
                                    Username = "unconfirmed",
                                    FirstName = "Unconfirmed",
                                    LastName = "Fixture",
                                    KeycloakSubjectConfirmed = false
                                };
                                db.AppUsers.Add(user);
                                db.UserRoles.Add(new UserRole
                                {
                                    UserId = user.Id,
                                    RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.PhysicalPersons)).Id
                                });
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
                        case "/__fixtures/bank-source-on": database.BankSourceEnabled = true; break;
                        case "/__fixtures/bank-source-off": database.BankSourceEnabled = false; break;
                        case "/__fixtures/notifications-seed":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                var languages = new CodeListDefinition { Name = "Languages", CreatedBy = "fixture" };
                                var packs = new CodeListDefinition { Name = "LanguagePacks", CreatedBy = "fixture" };
                                db.CodeListDefinitions.AddRange(languages, packs);
                                db.CodeLists.AddRange(
                                    new CodeList { Kategorija = "Languages", DefinitionId = languages.Id, Kod = "BS", Naziv = "Bosanski", KreiraoKorisnik = "fixture" },
                                    new CodeList { Kategorija = "Languages", DefinitionId = languages.Id, Kod = "EN", Naziv = "English", KreiraoKorisnik = "fixture" });
                                foreach (var (language, key, label) in new[] {
                                    ("BS", "notifications.fixture.title", "Rola dodijeljena"), ("BS", "notifications.fixture.body", "Odobrena rola {role}."),
                                    ("EN", "notifications.fixture.title", "Role granted"), ("EN", "notifications.fixture.body", "Role {role} was approved.") })
                                    db.CodeLists.Add(new CodeList { Kategorija = "LanguagePacks", DefinitionId = packs.Id, Kod = language + ":" + key, Naziv = key, LanguageCode = language, ResourceKey = key, ResourceLabel = label, KreiraoKorisnik = "fixture" });
                                var now = DateTime.UtcNow;
                                db.Notifications.AddRange(
                                    new ApplicationNotification { RecipientSubject = "fixture-user", RecipientEmail = "fixture@example.test", EventKey = "fixture.a", TitleKey = "notifications.fixture.title", BodyKey = "notifications.fixture.body", ParametersJson = "{\"role\":\"FL\"}", Route = "/app/profile", CreatedAt = now.AddMinutes(-1) },
                                    new ApplicationNotification { RecipientSubject = "fixture-user", RecipientEmail = "fixture@example.test", EventKey = "fixture.b", TitleKey = "notifications.fixture.title", BodyKey = "notifications.fixture.body", ParametersJson = "{\"role\":\"PL\"}", Route = "//evil.example", CreatedAt = now.AddMinutes(-2) },
                                    new ApplicationNotification { RecipientSubject = "fixture-user", RecipientEmail = "fixture@example.test", EventKey = "fixture.c", TitleKey = "notifications.fixture.title", BodyKey = "notifications.fixture.body", ParametersJson = "{\"role\":\"kapital\"}", CreatedAt = now.AddMinutes(-3), ReadAt = now.AddMinutes(-2) },
                                    new ApplicationNotification { RecipientSubject = "another-user", RecipientEmail = "other@example.test", EventKey = "fixture.d", TitleKey = "notifications.fixture.title", BodyKey = "notifications.fixture.body", ParametersJson = "{\"role\":\"OTHERS-ONLY\"}", CreatedAt = now });
                                await db.SaveChangesAsync();
                            }
                            break;
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
                                    Matbroj = "789456123",
                                    BasisOfConnection = "ZOB-2-V-5",
                                    ConnectionDescription = "Fixture relationship description",
                                    ConnectedWithBank = true,
                                    DateFrom = new(2026, 1, 1),
                                    Status = "Draft",
                                    CreatedBy = "fixture-author",
                                    IsActive = true,
                                    CreatedAt = new DateTime(2026, 1, 1).AddMinutes(index)
                                });
                                foreach (var seeded in db.ChangeTracker.Entries<RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity>().ToArray()) seeded.Property("CreatorSubject").CurrentValue = "fixture-author-subject"; // a proven creator other than the fixture user, so four-eyes verification can succeed
                                await db.SaveChangesAsync();
                            }
                            break;
                        case "/__fixtures/persons-entered-by-other":
                            await using (var scope = backend.Services.CreateAsyncScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
                                foreach (var person in db.RelatedPersons.IgnoreQueryFilters()) { person.CreatedBy = "fixture-author"; var creator = db.Entry(person).Property("CreatorSubject"); creator.CurrentValue = "fixture-author-subject"; creator.OriginalValue = "fixture-author-subject"; creator.IsModified = true; }
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
}
