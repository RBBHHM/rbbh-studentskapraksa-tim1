using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

public class AuditService(ConnectedPartiesDbContext db) : IAuditService
{
    public async Task LogAsync(AuditEntry entry)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TableName = entry.TableName,
            RecordId  = entry.RecordId,
            Action    = entry.Action,
            OldValues = entry.OldValues,
            NewValues = entry.NewValues,
            UserId    = entry.UserId,
            Username  = entry.Username,
            IpAddress = entry.IpAddress,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
