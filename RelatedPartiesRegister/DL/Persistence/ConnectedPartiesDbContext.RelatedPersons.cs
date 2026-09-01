using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    public virtual DbSet<RelatedPerson> RelatedPersons { get; set; }
    public virtual DbSet<FamilyMember> FamilyMembers { get; set; }
}
