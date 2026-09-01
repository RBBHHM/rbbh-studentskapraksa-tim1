using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/period-lock")]
[Authorize(Roles = "regulatory-reporting")]
public class PeriodLockController(
    IPeriodLockRepository periodLockRepository,
    IUnlockRequestRepository unlockRequestRepository,
    IPeriodLockService periodLockService,
    IAuditService auditService,
    IEmailService emailService,
    IConfiguration configuration) : ControllerBase
{
    private string CurrentUser() =>
        User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? "system";

    // Functional access is assigned directly to users and is intentionally
    // independent of their organizational unit.
    private static string? CurrentDepartment() => null;

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentState([FromQuery] string? department = null)
    {
        var now = DateTime.UtcNow;
        var dept = CurrentDepartment();
        var currentLock = await periodLockRepository.GetCurrentAsync(dept);
        var isLocked = await periodLockRepository.IsCurrentPeriodLockedAsync(dept);

        return Ok(new
        {
            year = now.Year,
            month = now.Month,
            monthDisplay = periodLockService.GetMonthDisplayName(now.Year, now.Month),
            isLocked,
            lockedBy = currentLock?.LockedBy,
            lockedAt = currentLock?.LockedAt,
            department = dept
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetPeriodStatus(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? department = null)
    {
        if (month < 1 || month > 12 || year < 2020)
            return BadRequest(Err("Nevažeći period."));

        var dept = CurrentDepartment();
        var isLocked = await periodLockRepository.IsPeriodLockedAsync(year, month, dept);
        var periodLock = await periodLockRepository.GetByPeriodAsync(year, month, dept);

        return Ok(new
        {
            year,
            month,
            monthDisplay = periodLockService.GetMonthDisplayName(year, month),
            isLocked,
            lockedBy = periodLock?.LockedBy,
            lockedAt = periodLock?.LockedAt,
            department = dept
        });
    }

    [HttpPost("lock")]
    public async Task<IActionResult> LockPeriod([FromBody] PeriodTargetDto? dto = null)
    {
        var now = DateTime.UtcNow;
        int year = dto?.Year ?? now.Year;
        int month = dto?.Month ?? now.Month;
        var dept = CurrentDepartment();

        var periodLock = await periodLockRepository.GetByPeriodAsync(year, month, dept);

        if (periodLock != null && periodLock.IsLocked)
            return BadRequest(Err("Period je već zaključan."));

        var username = CurrentUser();

        if (periodLock == null)
        {
            periodLock = new PeriodLock
            {
                Year = year, Month = month, IsLocked = true,
                Department = dept,
                LockedBy = username, LockedAt = now,
                CreatedBy = username, CreatedAt = now, IsActive = true
            };
            await periodLockRepository.CreateAsync(periodLock);
        }
        else
        {
            periodLock.IsLocked = true;
            periodLock.LockedBy = username;
            periodLock.LockedAt = now;
            periodLock.ModifiedBy = username;
            await periodLockRepository.UpdateAsync(periodLock);
        }

        await auditService.LogAsync(new AuditEntry
        {
            TableName = "PeriodLock", RecordId = $"{year}-{month:D2}",
            Action = "PERIOD_LOCK",
            NewValues = JsonSerializer.Serialize(new { year, month, department = dept, lockedBy = username }),
            UserId = username, Username = username,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        var deptLabel = dept is not null ? $" ({dept})" : "";
        return Ok(new { message = $"Period {periodLockService.GetMonthDisplayName(year, month)}{deptLabel} uspješno zaključan." });
    }

    [HttpPost("unlock")]
    public async Task<IActionResult> UnlockPeriod([FromBody] PeriodTargetDto? dto = null)
    {
        var now = DateTime.UtcNow;
        int year = dto?.Year ?? now.Year;
        int month = dto?.Month ?? now.Month;
        var dept = CurrentDepartment();

        var periodLock = await periodLockRepository.GetByPeriodAsync(year, month, dept);
        bool isPast = year < now.Year || (year == now.Year && month < now.Month);

        if (periodLock == null && isPast)
        {
            var username = CurrentUser();
            periodLock = new PeriodLock
            {
                Year = year, Month = month, IsLocked = false,
                Department = dept,
                UnlockedBy = username, UnlockedAt = now,
                CreatedBy = username, CreatedAt = now, IsActive = true
            };
            await periodLockRepository.CreateAsync(periodLock);

            await unlockRequestRepository.ApproveAllPendingAsync(year, month, username);

            await auditService.LogAsync(new AuditEntry
            {
                TableName = "PeriodLock", RecordId = $"{year}-{month:D2}",
                Action = "PERIOD_UNLOCK",
                NewValues = JsonSerializer.Serialize(new { isLocked = false, department = dept, unlockedBy = username }),
                UserId = username, Username = username,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            var label1 = dept is not null ? $" ({dept})" : "";
            return Ok(new { message = $"Period {periodLockService.GetMonthDisplayName(year, month)}{label1} uspješno otključan." });
        }

        if (periodLock == null || !periodLock.IsLocked)
        {
            if (!isPast)
                return BadRequest(Err("Period nije zaključan."));
        }

        {
            var username = CurrentUser();
            periodLock!.IsLocked = false;
            periodLock.UnlockedBy = username;
            periodLock.UnlockedAt = now;
            periodLock.ModifiedBy = username;
            await periodLockRepository.UpdateAsync(periodLock);

            await unlockRequestRepository.ApproveAllPendingAsync(year, month, username);

            await auditService.LogAsync(new AuditEntry
            {
                TableName = "PeriodLock", RecordId = $"{year}-{month:D2}",
                Action = "PERIOD_UNLOCK",
                OldValues = JsonSerializer.Serialize(new { isLocked = true, lockedBy = periodLock.LockedBy }),
                NewValues = JsonSerializer.Serialize(new { isLocked = false, department = dept, unlockedBy = username }),
                UserId = username, Username = username,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            var label2 = dept is not null ? $" ({dept})" : "";
            return Ok(new { message = $"Period {periodLockService.GetMonthDisplayName(year, month)}{label2} uspješno otključan." });
        }
    }

    [HttpPost("request-unlock")]
    public async Task<IActionResult> RequestUnlock([FromBody] RequestUnlockDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length < 10 || dto.Reason.Length > 500)
            return BadRequest(Err("Razlog je obavezan, minimalno 10 i maksimalno 500 karaktera.", "reason"));

        var now = DateTime.UtcNow;
        int year = dto.Year ?? now.Year;
        int month = dto.Month ?? now.Month;

        if (month < 1 || month > 12 || year < 2020)
            return BadRequest(Err("Nevažeći period."));

        var username = CurrentUser();
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "korisnik@raiffeisenbank.ba";

        var request = new UnlockRequest
        {
            RequestedBy = username, RequestedByEmail = email,
            Year = year, Month = month,
            Reason = dto.Reason, Status = "PENDING",
            CreatedBy = username, CreatedAt = now, IsActive = true
        };

        await unlockRequestRepository.CreateAsync(request);

        await auditService.LogAsync(new AuditEntry
        {
            TableName = "PeriodLock", RecordId = $"{year}-{month:D2}",
            Action = "UNLOCK_REQUEST",
            NewValues = JsonSerializer.Serialize(new
            {
                requestedBy = username, year, month,
                reason = dto.Reason[..Math.Min(dto.Reason.Length, 100)]
            }),
            UserId = username, Username = username,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        var adminEmail = configuration["Email:AdminEmail"] ?? "admin@raiffeisenbank.ba";
        await emailService.SendUnlockRequestAsync(adminEmail, username, year, month, dto.Reason);

        return Ok(new { message = "Zahtjev uspješno poslan administratoru." });
    }

    /// <summary>Returns unlock requests. status="" returns ALL, status="PENDING" filters.</summary>
    [HttpGet("unlock-requests")]
    public async Task<IActionResult> GetUnlockRequests(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Allow empty string to mean "all statuses"
        var statusFilter = string.IsNullOrWhiteSpace(status) ? null : status;
        var (items, total) = await unlockRequestRepository.GetPagedAsync(statusFilter, page, pageSize);

        var mapped = items.Select(r => new
        {
            id = r.Id,
            requestedBy = r.RequestedBy,
            year = r.Year,
            month = r.Month,
            monthDisplay = periodLockService.GetMonthDisplayName(r.Year, r.Month),
            reason = r.Reason,
            status = r.Status,
            adminNote = r.AdminNote,
            processedBy = r.ProcessedBy,
            processedAt = r.ProcessedAt,
            createdAt = r.CreatedAt
        });

        return Ok(new { requests = mapped, total, page, pageSize });
    }

    [HttpPost("unlock-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(
        [FromRoute] Guid id,
        [FromBody] AdminActionDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Length < 10)
            return BadRequest(Err("Razlog odbijanja je obavezan (min. 10 karaktera).", "note"));

        var username = CurrentUser();
        var success  = await unlockRequestRepository.RejectAsync(id, dto.Note.Trim(), username);

        if (!success)
            return NotFound(Err("Zahtjev nije pronađen ili nije u statusu Na čekanju."));

        await auditService.LogAsync(new AuditEntry
        {
            TableName = "PeriodLock", RecordId = id.ToString(),
            Action    = "UNLOCK_REQUEST_REJECTED",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { id, note = dto.Note, rejectedBy = username }),
            UserId = username, Username = username,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        return Ok(new { message = "Zahtjev je odbijen." });
    }

    [HttpPost("unlock-requests/{id:guid}/request-info")]
    public async Task<IActionResult> RequestMoreInfo(
        [FromRoute] Guid id,
        [FromBody] AdminActionDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Length < 10)
            return BadRequest(Err("Poruka korisniku je obavezna (min. 10 karaktera).", "note"));

        // Dohvati zahtjev PRIJE update-a — potreban email i podaci korisnika
        var request  = await unlockRequestRepository.GetByIdAsync(id);
        var username = CurrentUser();
        var success  = await unlockRequestRepository.RequestMoreInfoAsync(id, dto.Note.Trim(), username);

        if (!success)
            return NotFound(Err("Zahtjev nije pronađen ili nije u statusu Na čekanju."));

        await auditService.LogAsync(new AuditEntry
        {
            TableName = "PeriodLock", RecordId = id.ToString(),
            Action    = "UNLOCK_REQUEST_NEEDS_INFO",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { id, note = dto.Note, askedBy = username }),
            UserId = username, Username = username,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        // Email korisniku s adminovom porukom/pitanjem
        if (request is not null)
        {
            await emailService.SendNeedsInfoNotificationAsync(
                    request.RequestedByEmail, request.RequestedBy,
                    request.Year, request.Month, dto.Note.Trim(), request.Id);
        }

        return Ok(new { message = "Zahtjev za više informacija poslan." });
    }

    [HttpPost("unlock-requests/{id:guid}/respond")]
    public async Task<IActionResult> RespondToRequest(
        [FromRoute] Guid id,
        [FromBody] RespondDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Length < 10)
            return BadRequest(Err("Odgovor je obavezan (min. 10 karaktera).", "message"));

        var original = await unlockRequestRepository.GetByIdAsync(id);
        if (original is null)
            return NotFound(Err("Zahtjev nije pronađen."));

        var now      = DateTime.UtcNow;
        var username = CurrentUser();
        var email    = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                       ?? original.RequestedByEmail;

        // Kreiraj novi PENDING zahtjev s korisnikovim odgovorom
        var response = new UnlockRequest
        {
            RequestedBy      = username,
            RequestedByEmail = email,
            Year             = original.Year,
            Month            = original.Month,
            Reason           = $"[ODGOVOR] {dto.Message.Trim()}",
            Status           = "PENDING",
            CreatedBy        = username,
            CreatedAt        = now,
            IsActive         = true
        };
        await unlockRequestRepository.CreateAsync(response);

        await auditService.LogAsync(new AuditEntry
        {
            TableName = "PeriodLock", RecordId = id.ToString(),
            Action    = "UNLOCK_REQUEST",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "user_response", originalRequestId = id,
                requestedBy = username, year = original.Year, month = original.Month
            }),
            UserId = username, Username = username,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        // Email adminu s korisnikovim odgovorom
        var adminEmail = configuration["Email:AdminEmail"] ?? "admin@raiffeisenbank.ba";
        await emailService.SendUserResponseAsync(
                adminEmail, username, original.Year, original.Month, dto.Message.Trim());

        return Ok(new { message = "Odgovor je poslan administratoru." });
    }

    private static object Err(string message, string? field = null) =>
        new { errors = new[] { new ErrField(field, message) } };
}

public class RequestUnlockDto
{
    public string Reason { get; set; } = null!;
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class PeriodTargetDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class AdminActionDto
{
    public string Note { get; set; } = null!;
}

public class RespondDto
{
    public string Message { get; set; } = null!;
}

// Writes string values without Unicode-escaping non-ASCII characters so that
// JsonSerializer.Serialize(bad.Value) in integration tests produces literal Bosnian chars.
internal sealed class UnsafeStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f");
        writer.WriteRawValue($"\"{escaped}\"", skipInputValidation: true);
    }
}

internal sealed record ErrField(
    string? Field,
    [property: JsonConverter(typeof(UnsafeStringConverter))] string Message);
