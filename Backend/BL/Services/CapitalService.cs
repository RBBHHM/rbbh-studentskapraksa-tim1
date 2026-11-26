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
    private CancellationToken RequestCancellation => actor?.HttpContext?.RequestAborted ?? default;
    public async Task<Result<CapitalDTO?>> Get() => Result<CapitalDTO?>.Success(await CapitalHistory.AsOf(db, DateTime.UtcNow.Date, RequestCancellation));
    public async Task<Result<CapitalDTO?>> AsOf(DateTime date, CancellationToken ct) => Result<CapitalDTO?>.Success(await CapitalHistory.AsOf(db, date, ct));
    public async Task<ReferencePage<CapitalHistoryDTO>> History(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 1_000_000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = CapitalHistory.Query(db); var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(row => row.EffectiveDate).ThenByDescending(row => row.Revision).ThenByDescending(row => row.RecordedAt).ThenBy(row => row.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }
    public async Task<Result<CapitalDTO>> Upsert(UpdateCapitalDTO dto, string korisnik)
    {
        const decimal maximum = 9_999_999_999_999_999.99m;
        var values = new[] { dto.RegulatoryCapital, dto.CoreCapital, dto.SupplementaryCapital };
        if (values.Any(value => value is null || value < 0 || value > maximum || decimal.Round(value.Value, 2) != value.Value))
            return Result<CapitalDTO>.ValidationError(BackendMessages.Message("validation.capital.amounts"));
        if (!dto.CapitalDate.HasValue) return Result<CapitalDTO>.ValidationError(BackendMessages.Message("validation.capital.date.required"));
        if (!db.Database.IsRelational())
            return await SaveRevision(dto, korisnik);

        long savedId = 0;
        return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
            async _ =>
            {
                db.ChangeTracker.Clear();
                savedId = 0;
                var result = await SaveRevision(dto, korisnik);
                savedId = result.Value!.Id;
                return result;
            },
            ct => db.CapitalRevisions.AnyAsync(row => row.Id == savedId, ct),
            RequestCancellation);
    }

    private async Task<Result<CapitalDTO>> SaveRevision(UpdateCapitalDTO dto, string korisnik)
    {
        var row = new CapitalRevision { RegulatoryCapital = dto.RegulatoryCapital!.Value, CoreCapital = dto.CoreCapital!.Value,
            SupplementaryCapital = dto.SupplementaryCapital!.Value, EffectiveDate = dto.CapitalDate!.Value.Date,
            RecordedBy = korisnik, ActorSubject = actor?.HttpContext is { } context ? Helpers.AuthenticatedActor.RequireSubject(context.User) : null };
        var previous = await CapitalHistory.AsOf(db, row.EffectiveDate, RequestCancellation);
        var before = previous is null ? null : new
        {
            previous.Id, EffectiveDate = previous.CapitalDate, previous.RegulatoryCapital, previous.CoreCapital, previous.SupplementaryCapital
        };
        db.CapitalRevisions.Add(row);
        await db.SaveChangesAsync(RequestCancellation);
        db.AuditLogs.Add(Helpers.BusinessAudit.Create("Capital", row.Id.ToString(), "UPSERT", korisnik, actor, oldValues: before, newValues: new
        { row.Id, row.EffectiveDate, row.RegulatoryCapital, row.CoreCapital, row.SupplementaryCapital }));
        await db.SaveChangesAsync(RequestCancellation);
        return Result<CapitalDTO>.Success(new() { Id = row.Id, CapitalDate = row.EffectiveDate, RegulatoryCapital = row.RegulatoryCapital,
            CoreCapital = row.CoreCapital, SupplementaryCapital = row.SupplementaryCapital });
    }
}
