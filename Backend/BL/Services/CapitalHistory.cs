using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

internal static class CapitalHistory
{
    internal static IQueryable<CapitalHistoryDTO> Query(ConnectedPartiesDbContext db) =>
        db.CapitalRevisions.AsNoTracking().Select(row => new CapitalHistoryDTO
        {
            Id = row.Id, Revision = row.Revision, EffectiveDate = row.EffectiveDate, RegulatoryCapital = row.RegulatoryCapital,
            CoreCapital = row.CoreCapital, SupplementaryCapital = row.SupplementaryCapital, RecordedAt = row.RecordedAt, RecordedBy = row.RecordedBy, Source = "revision"
        }).Concat(db.Capitals.AsNoTracking().Select(row => new CapitalHistoryDTO
        {
            Id = row.Id, Revision = 0, EffectiveDate = row.CapitalDate, RegulatoryCapital = row.RegulatoryCapital,
            CoreCapital = row.CoreCapital, SupplementaryCapital = row.SupplementaryCapital, RecordedAt = row.ModifiedAt ?? row.CreatedAt,
            RecordedBy = row.ModifiedBy ?? row.CreatedBy, Source = "legacy"
        }));
    internal static async Task<CapitalDTO?> AsOf(ConnectedPartiesDbContext db, DateTime date, CancellationToken ct)
    {
        date = date.Date;
        var row = await Query(db).Where(row => row.EffectiveDate <= date).OrderByDescending(row => row.EffectiveDate)
            .ThenByDescending(row => row.Revision).ThenByDescending(row => row.RecordedAt).ThenBy(row => row.Id).FirstOrDefaultAsync(ct);
        return row is null ? null : new() { Id = row.Id, RegulatoryCapital = row.RegulatoryCapital, CoreCapital = row.CoreCapital,
            SupplementaryCapital = row.SupplementaryCapital, CapitalDate = row.EffectiveDate };
    }
}
