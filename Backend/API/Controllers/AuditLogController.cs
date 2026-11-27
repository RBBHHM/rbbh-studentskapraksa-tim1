using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.DL.Entities.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = ApplicationPolicies.AdminOnly)]
public class AuditLogController(ConnectedPartiesDbContext db) : ControllerBase
{
    private static readonly Dictionary<string, string> ActionDisplayNames = new()
    {
        ["INSERT"]          = "Unos",
        ["UPDATE"]          = "Izmjena",
        ["DELETE"]          = "Brisanje",
        ["VERIFY"]          = "Verifikacija",
        ["CREATE"]          = "Kreiranje korisnika",
        ["DEACTIVATE"]      = "Deaktivacija korisnika",
        ["REACTIVATE"]      = "Reaktivacija korisnika",
        ["ROLE_ASSIGN"]     = "Dodjela role",
        ["ROLE_REMOVE"]     = "Uklanjanje role",
        ["ROLE_CATALOG_CREATE"] = "Kreiranje role",
        ["ROLE_CATALOG_UPDATE"] = "Izmjena role",
        ["LOCALIZATION_UPDATE"] = "Izmjena prijevoda",
        ["ROLE_CATALOG_DELETE"] = "Brisanje role",
        ["ADMIN_TRANSFER"]  = "Prenos admin prava",
        ["CODELIST_ADD"]    = "Dodavanje šifarnika",
        ["CODELIST_UPDATE"] = "Izmjena šifarnika",
        ["CODELIST_DELETE"] = "Brisanje šifarnika",
        ["PERIOD_LOCK"]                = "Zaključavanje perioda",
        ["PERIOD_UNLOCK"]              = "Otključavanje perioda",
        ["UNLOCK_REQUEST"]             = "Zahtjev za otključavanje",
        ["UNLOCK_REQUEST_REJECTED"]    = "Odbijanje zahtjeva",
        ["UNLOCK_REQUEST_NEEDS_INFO"]  = "Zahtjev za informacije",
        ["IMPORT"]                     = "Uvoz podataka",
        ["EXPORT"]                     = "Izvoz podataka",
        ["GENERATE"]                   = "Generisanje izvještaja",
        ["WORK_AUTH_INSERT"]           = "Dodjela ovlaštenja",
        ["WORK_AUTH_UPDATE"]           = "Izmjena ovlaštenja",
        ["WORK_AUTH_DELETE"]           = "Uklanjanje ovlaštenja"
    };

    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetLogs(
        [FromQuery] int      page       = 1,
        [FromQuery] int      pageSize   = 20,
        [FromQuery] string?  tableName  = null,
        [FromQuery] string?  action     = null,
        [FromQuery] string?  username   = null,
        [FromQuery] string?  search     = null,
        [FromQuery] DateTime? dateFrom  = null,
        [FromQuery] DateTime? dateTo    = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var english = Request.Headers.AcceptLanguage.ToString().StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(l => l.TableName == tableName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(username))
        {
            var term = username.Trim();
            query = db.Database.IsRelational()
                ? query.Where(l => EF.Functions.Like(l.Username, $"%{term}%"))
                : query.Where(l => l.Username.ToLower().Contains(term.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = db.Database.IsRelational()
                ? query.Where(l => EF.Functions.Like(l.Username, $"%{term}%")
                    || (l.Id.ToString() == term || EF.Functions.Like(l.NewValues ?? "", $"%{term}%") || EF.Functions.Like(l.OldValues ?? "", $"%{term}%")))
                : query.Where(l => l.Username.ToLower().Contains(term.ToLower())
                    || l.Id.ToString() == term || (l.OldValues ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (l.NewValues ?? "").Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (dateFrom.HasValue)
            query = query.Where(l => l.Timestamp >= DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc));

        if (dateTo.HasValue && dateTo.Value.Date < DateTime.MaxValue.Date)
            query = query.Where(l => l.Timestamp < DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1), DateTimeKind.Utc));

        var total = await query.CountAsync();

        var logEntities = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue))
            .Take(pageSize)
            .ToListAsync();
        var logs = logEntities.Select(l =>
        {
            var oldValues = l.OldValues;
            var newValues = l.NewValues;
            return new
            {
                id            = l.Id,
                tableName     = l.TableName,
                action        = l.Action,
                actionDisplay = ActionDisplayName(l.Action, english),
                areaDisplay   = AreaDisplayName(l.TableName, english),
                changeSummary = BuildChangeSummary(oldValues, newValues, english),
                oldValues,
                newValues,
                username      = l.Username,
                timestamp     = l.Timestamp
            };
        }).ToList();

        var referenceIds = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logEntities)
        {
            CollectReferences(log.OldValues, referenceIds);
            CollectReferences(log.NewValues, referenceIds);
        }

        var referenceNames = new Dictionary<string, Dictionary<long, string>>(StringComparer.OrdinalIgnoreCase);
        async Task LoadNames(string field, IQueryable<ReferenceName> source)
        {
            if (!referenceIds.TryGetValue(field, out var ids)) return;
            referenceNames[field] = await source.Where(item => ids.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, HttpContext.RequestAborted);
        }

        await LoadNames("LegalEntityId", db.LegalEntities.IgnoreQueryFilters()
            .Select(item => new ReferenceName { Id = item.Id, Name = item.Name }));
        await LoadNames("DefinitionId", db.CodeListDefinitions.IgnoreQueryFilters()
            .Select(item => new ReferenceName { Id = item.Id, Name = item.Name }));
        var persons = db.RelatedPersons.IgnoreQueryFilters()
            .Select(item => new ReferenceName { Id = item.Id, Name = item.FirstName + " " + item.LastName });
        await LoadNames("RelatedPersonId", persons);
        await LoadNames("RelatedToPersonId", persons);
        await LoadNames("ParentFamilyMemberId", db.FamilyMembers.IgnoreQueryFilters()
            .Select(item => new ReferenceName { Id = item.Id, Name = item.FirstName + " " + item.LastName }));
        if (referenceIds.TryGetValue("CapitalId", out var capitalIds))
        {
            var dates = await db.Capitals.IgnoreQueryFilters().Where(item => capitalIds.Contains(item.Id))
                .Select(item => new { item.Id, item.CapitalDate }).ToListAsync(HttpContext.RequestAborted);
            referenceNames["CapitalId"] = dates.ToDictionary(item => item.Id,
                item => item.CapitalDate.ToString(english ? "yyyy-MM-dd" : "dd.MM.yyyy.", System.Globalization.CultureInfo.InvariantCulture));
        }

        Dictionary<long, string> permissionCodes = [];
        if (logEntities.Any(log => log.TableName == "Role"))
        {
            permissionCodes = await db.Permissions.AsNoTracking()
                .ToDictionaryAsync(permission => permission.Id, permission => permission.Code, HttpContext.RequestAborted);
        }

        return Ok(new { logs, total, page, pageSize, permissionCodes, referenceNames });
    }

    private sealed class ReferenceName
    {
        public long Id { get; init; }
        public required string Name { get; init; }
    }

    private static readonly HashSet<string> ReferenceFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "LegalEntityId", "DefinitionId", "RelatedPersonId", "RelatedToPersonId", "ParentFamilyMemberId", "CapitalId"
    };

    private static void CollectReferences(string? snapshot, Dictionary<string, HashSet<long>> idsByField)
    {
        if (string.IsNullOrWhiteSpace(snapshot)) return;
        try
        {
            using var document = JsonDocument.Parse(snapshot);
            Visit(document.RootElement);
        }
        catch (JsonException)
        {
            // Older entries can contain text instead of a JSON snapshot.
        }

        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray()) Visit(item);
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (ReferenceFields.Contains(property.Name) && property.Value.ValueKind == JsonValueKind.Number &&
                        property.Value.TryGetInt64(out var id))
                    {
                        if (!idsByField.TryGetValue(property.Name, out var ids))
                            idsByField[property.Name] = ids = [];
                        ids.Add(id);
                    }
                    Visit(property.Value);
                }
            }
        }
    }

    private static string ActionDisplayName(string action, bool english)
    {
        var fallback = System.Globalization.CultureInfo.InvariantCulture.TextInfo
            .ToTitleCase(action.Replace('_', ' ').ToLowerInvariant());
        if (!english) return ActionDisplayNames.GetValueOrDefault(action, fallback);
        return action switch
        {
            "INSERT" => "Create", "UPDATE" => "Update", "DELETE" => "Delete", "VERIFY" => "Verify",
            "CREATE" => "Create user", "DEACTIVATE" => "Deactivate user", "REACTIVATE" => "Reactivate user",
            "ROLE_CATALOG_CREATE" => "Create role", "ROLE_CATALOG_UPDATE" => "Update role",
            "CODELIST_ADD" => "Create code list", "CODELIST_UPDATE" => "Update code list",
            "CODELIST_DELETE" => "Delete code list", "LOCALIZATION_UPDATE" => "Update translations",
            "WORK_AUTH_INSERT" => "Grant authorization", "WORK_AUTH_UPDATE" => "Update authorization",
            "WORK_AUTH_DELETE" => "Revoke authorization",
            "ROLE_ASSIGN" => "Assign access", "ROLE_REMOVE" => "Remove access", "ROLE_CATALOG_DELETE" => "Delete role", "IMPORT" => "Import data",
            "EXPORT" => "Export data", "GENERATE" => "Generate report", "PERIOD_LOCK" => "Lock period", "PERIOD_UNLOCK" => "Unlock period",
            "UNLOCK_REQUEST" => "Unlock request", "UNLOCK_REQUEST_REJECTED" => "Reject request",
            "UNLOCK_REQUEST_NEEDS_INFO" => "Request more information", _ => fallback
        };
    }

    private static string AreaDisplayName(string tableName, bool english) => tableName switch
    {
        "RelatedPerson" => english ? "Individuals" : "Fizička lica",
        "LegalEntity" => english ? "Legal entities" : "Pravna lica",
        "Limit" or "Limiti" => english ? "Limits" : "Limiti",
        "FamilyMember" => english ? "Individuals" : "Fizička lica",
        "Role" => english ? "Roles" : "Role",
        "AppUser" => english ? "Users" : "Korisnici",
        "PeriodLock" or "UnlockRequest" => english ? "Period management" : "Upravljanje periodom",
        "CodeList" => english ? "Code lists" : "Šifrarnici",
        "Report" => english ? "Regulatory reports" : "Regulatorni izvještaji", _ => tableName
    };

    private static string BuildChangeSummary(string? oldValues, string? newValues, bool english)
    {
        var value = string.IsNullOrWhiteSpace(newValues) ? oldValues : newValues;
        if (string.IsNullOrWhiteSpace(value)) return english ? "No additional details." : "Nema dodatnih detalja.";
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return value;
            return string.Join("; ", document.RootElement.EnumerateObject().Take(5).Select(property =>
                $"{property.Name}: {property.Value.ToString()}"));
        }
        catch (System.Text.Json.JsonException) { return value; }
    }

}
