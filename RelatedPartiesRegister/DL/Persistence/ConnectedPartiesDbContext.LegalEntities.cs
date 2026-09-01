// File: src/RBBH.ConnectedParties/DL/Entities/Persistence/ConnectedPartiesDbContext_LegalEntity.cs

using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    public virtual DbSet<RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity> LegalEntities { get; set; }
}
