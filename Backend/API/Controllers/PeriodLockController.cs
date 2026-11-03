using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Exceptions;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/period-lock")]
[Authorize(Policy = ApplicationPolicies.CapitalRead)]
public class PeriodLockController(
    IPeriodLockRepository periodLockRepository,
    IUnlockRequestRepository unlockRequestRepository,
    IPeriodLockService periodLockService,
    PeriodWorkflow workflow) : ControllerBase
{
    private string ActorSubject() =>
        Helpers.RegistryVerification.CreatorSubject(new HttpContextAccessor { HttpContext = HttpContext })
        ?? throw new MissingActorIdentityException();

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentState()
    {
        var now = DateTime.UtcNow;
        var currentLock = await periodLockRepository.GetCurrentAsync();
        var isLocked = await periodLockRepository.IsCurrentPeriodLockedAsync();

        return Ok(new
        {
            year = now.Year,
            month = now.Month,
            monthDisplay = periodLockService.GetMonthDisplayName(now.Year, now.Month),
            isLocked,
            lockedBy = currentLock?.LockedBy,
            lockedAt = currentLock?.LockedAt
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetPeriodStatus(
        [FromQuery] int year, [FromQuery] int month)
    {
        if (month < 1 || month > 12 || year < 2020 || year > 9999)
            return BadRequest(Err(BackendMessages.Message("errors.period.invalid")));

        var isLocked = await periodLockRepository.IsPeriodLockedAsync(year, month);
        var periodLock = await periodLockRepository.GetByPeriodAsync(year, month);

        return Ok(new
        {
            year,
            month,
            monthDisplay = periodLockService.GetMonthDisplayName(year, month),
            isLocked,
            lockedBy = periodLock?.LockedBy,
            lockedAt = periodLock?.LockedAt
        });
    }

    [HttpPost("lock")]
    [Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> LockPeriod([FromBody] PeriodTargetDto? dto = null)
    {
        var now = DateTime.UtcNow; var year = dto?.Year ?? now.Year; var month = dto?.Month ?? now.Month;
        return Command(() => workflow.SetLock(year, month, true, User, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted),
            BackendMessages.Message("messages.period.locked", periodLockService.GetMonthDisplayName(year, month)));
    }

    [HttpPost("unlock"), Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> UnlockPeriod([FromBody] PeriodTargetDto? dto = null)
    {
        var now = DateTime.UtcNow; var year = dto?.Year ?? now.Year; var month = dto?.Month ?? now.Month;
        return Command(() => workflow.SetLock(year, month, false, User, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted),
            BackendMessages.Message("messages.period.unlocked", periodLockService.GetMonthDisplayName(year, month)));
    }

    [HttpPost("request-unlock"), Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> RequestUnlock([FromBody] RequestUnlockDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500 || dto.Reason.Trim().Length < 10)
            return Task.FromResult<IActionResult>(BadRequest(Err(BackendMessages.Message("errors.period.reason.required"), "reason")));
        var now = DateTime.UtcNow;
        return Command(() => workflow.Request(dto.Year ?? now.Year, dto.Month ?? now.Month, dto.Reason, User,
            HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted), BackendMessages.Message("messages.period.request.sent"));
    }

    /// <summary>Returns unlock requests. status="" returns ALL, status="PENDING" filters.</summary>
    [HttpGet("unlock-requests")]
    public async Task<IActionResult> GetUnlockRequests(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return await ListUnlockRequests(status, page, pageSize, ActorSubject());
    }

    [HttpGet("unlock-requests/review")]
    [Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> GetReviewUnlockRequests([FromQuery] string? status = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20) => ListUnlockRequests(status, page, pageSize, null);

    private async Task<IActionResult> ListUnlockRequests(string? status, int page, int pageSize, string? owner)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        // Allow empty string to mean "all statuses"
        var statusFilter = string.IsNullOrWhiteSpace(status) ? null : status;
        var (items, total) = await unlockRequestRepository.GetPagedAsync(statusFilter, page, pageSize, owner);

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
    [Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> RejectRequest([FromRoute] Guid id, [FromBody] AdminActionDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Length > 1000 || dto.Note.Trim().Length < 10)
            return Task.FromResult<IActionResult>(BadRequest(Err(BackendMessages.Message("errors.period.rejection.note.required"), "note")));
        return Command(() => workflow.Process(id, true, dto.Note, User, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted), BackendMessages.Message("messages.period.request.rejected"));
    }

    [HttpPost("unlock-requests/{id:guid}/request-info"), Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> RequestMoreInfo([FromRoute] Guid id, [FromBody] AdminActionDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Length > 1000 || dto.Note.Trim().Length < 10)
            return Task.FromResult<IActionResult>(BadRequest(Err(BackendMessages.Message("errors.period.request.note.required"), "note")));
        return Command(() => workflow.Process(id, false, dto.Note, User, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted), BackendMessages.Message("messages.period.information.requested"));
    }

    [HttpPost("unlock-requests/{id:guid}/respond"), Authorize(Policy = ApplicationPolicies.CapitalWrite)]
    public Task<IActionResult> RespondToRequest([FromRoute] Guid id, [FromBody] RespondDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Length > 490 || dto.Message.Trim().Length < 10)
            return Task.FromResult<IActionResult>(BadRequest(Err(BackendMessages.Message("errors.period.response.length"), "message")));
        return Command(() => workflow.Respond(id, dto.Message, User, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted), BackendMessages.Message("messages.period.response.sent"));
    }

    private async Task<IActionResult> Command(Func<Task> command, string message)
    {
        try { await command(); return Ok(new { message }); }
        catch (BusinessRuleException error)
        {
            var body = new { errors = new[] { new ErrField(null, error.Message) }, code = error.Code };
            return error.Status switch { 400 => BadRequest(body), 404 => NotFound(body), 409 => Conflict(body), _ => StatusCode(error.Status, body) };
        }
        catch (DbUpdateConcurrencyException) { return Conflict(new { errors = new[] { new ErrField(null, BackendMessages.Message("errors.period.request.changed")) }, code = "request.concurrent_change" }); }
        catch (DbUpdateException error) when (error.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        { return Conflict(new { errors = new[] { new ErrField(null, BackendMessages.Message("errors.period.request.changed")) }, code = "request.concurrent_change" }); }
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

// Preserve literal Bosnian text while letting the JSON writer escape every
// control character, quote and backslash. Never construct raw JSON strings.
internal sealed class BusinessMessageStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(JsonEncodedText.Encode(value,
            System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping));
}

internal sealed record ErrField(
    string? Field,
    [property: JsonConverter(typeof(BusinessMessageStringConverter))] string Message);
