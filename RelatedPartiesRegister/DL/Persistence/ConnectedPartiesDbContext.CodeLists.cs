using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Limiti;
namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PeriodLockConfiguration());
        modelBuilder.ApplyConfiguration(new UnlockRequestConfiguration());
        modelBuilder.ApplyConfiguration(new RelatedPersonConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyMemberConfiguration());
        modelBuilder.ApplyConfiguration(new LegalEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LimitConfiguration());
        modelBuilder.ApplyConfiguration(new ReportConfiguration());
modelBuilder.ApplyConfiguration(new ClientLimitConfiguration());

    }
}