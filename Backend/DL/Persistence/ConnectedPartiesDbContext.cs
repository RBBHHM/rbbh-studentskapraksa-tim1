using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext : DbContext
{
    public ConnectedPartiesDbContext(DbContextOptions<ConnectedPartiesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RBBH.ConnectedParties.DL.Entities.Role.Role> Roles { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<RolePermission> RolePermissions { get; set; }
    public virtual DbSet<CodeList> CodeLists { get; set; }
    public virtual DbSet<CodeListDefinition> CodeListDefinitions { get; set; }
    public virtual DbSet<AppUser> AppUsers { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<Limit> Limiti { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CodeList>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.HasIndex(e => e.Kategorija);
            entity.HasQueryFilter(e => e.Aktivan);
        });
        modelBuilder.Entity<CodeListDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeycloakId).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.TableName);
            entity.HasIndex(e => e.Username);
            // Audit logs are never soft-deleted — no HasQueryFilter
        });

        modelBuilder.Entity<RBBH.ConnectedParties.DL.Entities.Role.Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasOne(e => e.Role)
                  .WithMany(role => role.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role)
                  .WithMany(role => role.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                  .WithMany(permission => permission.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    private void ProtectCreatorIdentity()
    {
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State == EntityState.Modified &&
                     (entry.Entity is DL.Entities.LegalEntity.LegalEntity || entry.Entity is DL.Entities.RelatedPersons.RelatedPerson)))
        {
            var creator = entry.Property("CreatorSubject");
            if (!Equals(creator.OriginalValue, creator.CurrentValue))
                throw new Exceptions.BusinessRuleException(409, "verification.creator_immutable", "Identitet autora nije moguće promijeniti.");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectCreatorIdentity();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectCreatorIdentity();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
