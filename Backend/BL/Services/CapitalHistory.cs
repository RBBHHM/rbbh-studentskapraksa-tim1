using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

internal static class CapitalHistory
{
    internal static CapitalDTO Map(Capital row) => new()
    {
        Id = row.Id, RegulatoryCapital = row.RegulatoryCapital, CoreCapital = row.CoreCapital,
        SupplementaryCapital = row.SupplementaryCapital, CapitalDate = row.CapitalDate,
        ValidTill = row.ValidTill, IsActive = row.IsActive, ModifiedAt = row.ModifiedAt
    };

    internal static async Task<CapitalDTO?> AsOf(ConnectedPartiesDbContext db, DateTime date, CancellationToken ct)
    {
        date = date.Date;
        var row = await db.Capitals.AsNoTracking().Where(row => row.CapitalDate <= date && row.ValidTill >= date)
            .OrderByDescending(row => row.CapitalDate).ThenByDescending(row => row.Id).FirstOrDefaultAsync(ct);
        return row is null ? null : Map(row);
    }
}
