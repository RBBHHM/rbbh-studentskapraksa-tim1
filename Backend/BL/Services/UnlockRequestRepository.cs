using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

public class UnlockRequestRepository(ConnectedPartiesDbContext db) : IUnlockRequestRepository
{
    public async Task<UnlockRequest> CreateAsync(UnlockRequest request)
    {
        db.UnlockRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    public async Task<(List<UnlockRequest> Items, int Total)> GetPagedAsync(
        string? status, int page, int pageSize)
    {
        var query = db.UnlockRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status.ToUpper());

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    public async Task<UnlockRequest?> GetByIdAsync(Guid id) =>
        await db.UnlockRequests.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<bool> RejectAsync(Guid id, string adminNote, string processedBy)
    {
        var now = DateTime.UtcNow;
        var request = await db.UnlockRequests.FirstOrDefaultAsync(r => r.Id == id && r.Status == "PENDING");
        if (request is null) return false;
        request.Status = "REJECTED";
        request.AdminNote = adminNote;
        request.ProcessedBy = processedBy;
        request.ProcessedAt = now;
        request.ModifiedBy = processedBy;
        request.ModifiedAt = now;
        db.UnlockRequests.Update(request);
        return await db.SaveChangesAsync() > 0;
    }

    public async Task<bool> RequestMoreInfoAsync(Guid id, string adminNote, string processedBy)
    {
        var now = DateTime.UtcNow;
        var request = await db.UnlockRequests.FirstOrDefaultAsync(r => r.Id == id && r.Status == "PENDING");
        if (request is null) return false;
        request.Status = "NEEDS_INFO";
        request.AdminNote = adminNote;
        request.ProcessedBy = processedBy;
        request.ProcessedAt = now;
        request.ModifiedBy = processedBy;
        request.ModifiedAt = now;
        db.UnlockRequests.Update(request);
        return await db.SaveChangesAsync() > 0;
    }

    public async Task ApproveAllPendingAsync(int year, int month, string approvedBy)
    {
        var now = DateTime.UtcNow;
        var requests = await db.UnlockRequests
            .Where(r => r.Year == year && r.Month == month && r.Status == "PENDING")
            .ToListAsync();
        foreach (var request in requests)
        {
            request.Status = "APPROVED";
            request.ProcessedBy = approvedBy;
            request.ProcessedAt = now;
            request.ModifiedBy = approvedBy;
            request.ModifiedAt = now;
        }
        db.UnlockRequests.UpdateRange(requests);
        await db.SaveChangesAsync();
    }
}
