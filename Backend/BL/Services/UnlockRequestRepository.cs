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
        string? status, int page, int pageSize, string? ownerSubject = null)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.UnlockRequests.AsQueryable();
        if (ownerSubject is not null) query = query.Where(row => row.RequestedBySubject == ownerSubject);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status.ToUpper());

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue))
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    public async Task<UnlockRequest?> GetByIdAsync(Guid id) =>
        await db.UnlockRequests.FirstOrDefaultAsync(r => r.Id == id);

    public async Task RespondAsync(Guid id, string ownerSubject, UnlockRequest response, AuditEntry audit, CancellationToken cancellationToken)
    {
        var original = await db.UnlockRequests.SingleOrDefaultAsync(row => row.Id == id && row.RequestedBySubject == ownerSubject, cancellationToken)
            ?? throw new Exceptions.BusinessRuleException(404, "request.not_found", "Zahtjev nije pronađen.");
        if (original.Status != "NEEDS_INFO" || await db.UnlockRequests.AnyAsync(row => row.ParentRequestId == id, cancellationToken))
            throw new Exceptions.BusinessRuleException(409, "request.invalid_state", "Zahtjev više ne očekuje dopunu.");
        original.ModifiedAt = DateTime.UtcNow;
        original.ModifiedBy = response.CreatedBy;
        response.ParentRequestId = original.Id;
        response.RequestedBySubject = ownerSubject;
        db.UnlockRequests.Add(response);
        db.AuditLogs.Add(new DL.Entities.Audit.AuditLog
        {
            TableName = audit.TableName, RecordId = audit.RecordId, Action = audit.Action,
            UserId = audit.UserId, Username = audit.Username, IpAddress = audit.IpAddress,
            OldValues = audit.OldValues, NewValues = audit.NewValues, Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

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
