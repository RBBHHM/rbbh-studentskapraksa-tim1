using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = "application-administration")]
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
                    || EF.Functions.Like(l.RecordId, $"%{term}%"))
                : query.Where(l => l.Username.ToLower().Contains(term.ToLower())
                    || l.RecordId.ToLower().Contains(term.ToLower()));
        }

        if (dateFrom.HasValue)
            query = query.Where(l => l.Timestamp >= DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc));

        if (dateTo.HasValue)
            query = query.Where(l => l.Timestamp < DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1), DateTimeKind.Utc));

        var total = await query.CountAsync();

        var logEntities = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var logs = logEntities.Select(l => new
            {
                id            = l.Id,
                tableName     = l.TableName,
                recordId      = l.RecordId,
                action        = l.Action,
                actionDisplay = ActionDisplayName(l.Action, english),
                areaDisplay   = AreaDisplayName(l.TableName, english),
                changeSummary = BuildChangeSummary(l.OldValues, l.NewValues, english),
                oldValues     = l.OldValues,
                newValues     = l.NewValues,
                username      = l.Username,
                ipAddress     = l.IpAddress,
                timestamp     = l.Timestamp
            }).ToList();

        return Ok(new { logs, total, page, pageSize });
    }

    private static string ActionDisplayName(string action, bool english)
    {
        if (!english) return ActionDisplayNames.GetValueOrDefault(action, action);
        return action switch
        {
            "INSERT" => "Create", "UPDATE" => "Update", "DELETE" => "Delete", "VERIFY" => "Verify",
            "CREATE" => "Create user", "DEACTIVATE" => "Deactivate user", "REACTIVATE" => "Reactivate user",
            "ROLE_ASSIGN" => "Assign access", "ROLE_REMOVE" => "Remove access", "IMPORT" => "Import data",
            "EXPORT" => "Export data", "PERIOD_LOCK" => "Lock period", "PERIOD_UNLOCK" => "Unlock period",
            "UNLOCK_REQUEST" => "Unlock request", "UNLOCK_REQUEST_REJECTED" => "Reject request",
            "UNLOCK_REQUEST_NEEDS_INFO" => "Request more information", _ => action
        };
    }

    private static string AreaDisplayName(string tableName, bool english) => tableName switch
    {
        "RelatedPerson" => english ? "Individuals" : "Fizička lica",
        "LegalEntity" => english ? "Legal entities" : "Pravna lica",
        "Limit" or "Limiti" => english ? "Limits" : "Limiti",
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
