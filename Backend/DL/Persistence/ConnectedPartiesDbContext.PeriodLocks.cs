using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    public virtual DbSet<global::RBBH.ConnectedParties.DL.Entities.PeriodLock.PeriodLock> PeriodLocks { get; set; }
    public virtual DbSet<UnlockRequest> UnlockRequests { get; set; }
}