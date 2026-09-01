using Microsoft.EntityFrameworkCore;
using ReportEntities = RBBH.ConnectedParties.DL.Entities.Report;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    public virtual DbSet<ReportEntities.Report> Reports { get; set; }
    public virtual DbSet<ReportEntities.ClientLimit> ClientLimits { get; set; }
}