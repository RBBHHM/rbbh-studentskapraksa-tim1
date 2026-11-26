using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class CapitalService(ConnectedPartiesDbContext db, IHttpContextAccessor? actor = null) : ICapitalService
{
    private static readonly SemaphoreSlim InMemoryMutationLock = new(1, 1);
    private CancellationToken RequestCancellation => actor?.HttpContext?.RequestAborted ?? default;
    private static DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Europe/Sarajevo").Date;

    public async Task<Result<CapitalDTO?>> Get()
    {
        var row = await db.Capitals.AsNoTracking().SingleOrDefaultAsync(row => row.IsActive, RequestCancellation);
        return Result<CapitalDTO?>.Success(row is null ? null : CapitalHistory.Map(row));
    }

    public async Task<Result<CapitalDTO?>> AsOf(DateTime date, CancellationToken ct) =>
        Result<CapitalDTO?>.Success(await CapitalHistory.AsOf(db, date, ct));

    public async Task<ReferencePage<CapitalHistoryDTO>> History(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Capitals.AsNoTracking().Where(row => !row.IsActive);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(row => row.CapitalDate).ThenByDescending(row => row.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(row => new CapitalHistoryDTO
            {
                Id = row.Id, EffectiveDate = row.CapitalDate, ValidTill = row.ValidTill,
                RegulatoryCapital = row.RegulatoryCapital, CoreCapital = row.CoreCapital,
                SupplementaryCapital = row.SupplementaryCapital, RecordedAt = row.CreatedAt, RecordedBy = row.CreatedBy
            }).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    public async Task<Result<CapitalDTO>> Upsert(UpdateCapitalDTO dto, string username, bool create = false)
    {
        const decimal maximum = 9_999_999_999_999_999.99m;
        var values = new[] { dto.RegulatoryCapital, dto.CoreCapital, dto.SupplementaryCapital };
        if (values.Any(value => value is null || value < 0 || value > maximum || decimal.Round(value.Value, 2) != value.Value))
            return Result<CapitalDTO>.ValidationError(BackendMessages.Message("validation.capital.amounts"), code: "capital.amounts");
        if (dto.ValidTill is null)
            return Result<CapitalDTO>.ValidationError(BackendMessages.Message("validation.capital.date.required"));

        var now = DateTime.UtcNow;
        var today = Today;
        if (dto.ValidTill.Value.Date < today)
            return InvalidEndDate();

        if (!db.Database.IsRelational())
        {
            await InMemoryMutationLock.WaitAsync(RequestCancellation);
            try { return await Save(dto, username, create, now, today); }
            finally { InMemoryMutationLock.Release(); }
        }

        long? auditId = null;
        CapitalDTO? saved = null;
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (saved is not null && auditId is { } id && await db.AuditLogs.AnyAsync(row => row.Id == id, RequestCancellation))
                return Result<CapitalDTO>.Success(saved);

            db.ChangeTracker.Clear();
            auditId = null;
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, RequestCancellation);
            var result = await Save(dto, username, create, now, today, id => auditId = id);
            saved = result.Value;
            await transaction.CommitAsync(RequestCancellation);
            return result;
        });
    }

    private static Result<CapitalDTO> InvalidEndDate() =>
        Result<CapitalDTO>.ValidationError(BackendMessages.Message("validation.capital.date.past"), code: "capital.valid_to_past");

    private async Task<Result<CapitalDTO>> Save(UpdateCapitalDTO dto, string username, bool create, DateTime now, DateTime today,
        Action<long>? captureAuditId = null)
    {
        var previous = await db.Capitals.SingleOrDefaultAsync(row => row.IsActive, RequestCancellation);
        if (!create && (previous is null || dto.ExpectedId != previous.Id || dto.ExpectedModifiedAt != previous.ModifiedAt))
            throw new Exceptions.BusinessRuleException(409, "request.concurrent_change");
        var from = create ? today : previous!.CapitalDate;
        if ((create && previous?.CapitalDate > today) || dto.ValidTill!.Value.Date < today || dto.ValidTill.Value.Date < from)
            return InvalidEndDate();

        var before = previous is null ? null : CapitalHistory.Map(previous);
        var row = create ? new Capital { CapitalDate = from, CreatedBy = username, CreatedAt = now } : previous!;
        if (create)
        {
            if (previous is not null)
            {
                previous.IsActive = false;
                if (previous.ValidTill >= from) previous.ValidTill = from.AddDays(-1);
                previous.ModifiedAt = now;
                previous.ModifiedBy = username;
                if (db.Database.IsRelational()) await db.SaveChangesAsync(RequestCancellation);
            }
            db.Capitals.Add(row);
        }
        else
        {
            row.ModifiedAt = now;
            row.ModifiedBy = username;
        }

        row.ValidTill = dto.ValidTill.Value.Date;
        row.RegulatoryCapital = dto.RegulatoryCapital!.Value;
        row.CoreCapital = dto.CoreCapital!.Value;
        row.SupplementaryCapital = dto.SupplementaryCapital!.Value;
        if (db.Database.IsRelational()) await db.SaveChangesAsync(RequestCancellation);
        var after = CapitalHistory.Map(row);
        var audit = Helpers.BusinessAudit.Create("Capital", row.Id.ToString(), create ? "INSERT" : "UPDATE", username, actor, before, after);
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync(RequestCancellation);
        captureAuditId?.Invoke(audit.Id);
        return Result<CapitalDTO>.Success(after);
    }
}
