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
    public DbSet<DL.Entities.Authorization.RoleRequest> RoleRequests => Set<DL.Entities.Authorization.RoleRequest>();
    public DbSet<DL.Entities.Notifications.ApplicationNotification> Notifications => Set<DL.Entities.Notifications.ApplicationNotification>();
    public DbSet<DL.Entities.Notifications.NotificationDelivery> NotificationDeliveries => Set<DL.Entities.Notifications.NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Provider subjects are opaque, case-sensitive identifiers. Apply the same
        // indexed SQL comparison to identity resolution and every owner-scoped query.
        const string subjectCollation = "Latin1_General_100_BIN2";
        modelBuilder.Entity<AppUser>().Property(row => row.KeycloakId).UseCollation(subjectCollation);
        modelBuilder.Entity<DL.Entities.Authorization.RoleRequest>().Property(row => row.RequesterSubject).UseCollation(subjectCollation);
        modelBuilder.Entity<DL.Entities.Notifications.ApplicationNotification>().Property(row => row.RecipientSubject).UseCollation(subjectCollation);
        modelBuilder.Entity<DL.Entities.PeriodLock.UnlockRequest>().Property(row => row.RequestedBySubject).UseCollation(subjectCollation);

        modelBuilder.Entity<DL.Entities.Authorization.RoleRequest>(entity =>
        {
            entity.HasIndex(row => new { row.RequesterSubject, row.ClientRequestId }).IsUnique();
            entity.HasIndex(row => row.RequesterSubject).IsUnique().HasFilter("[Status] = N'pending'");
            entity.HasIndex(row => new { row.Status, row.CreatedAt });
        });
        modelBuilder.Entity<DL.Entities.Authorization.RequestedRole>(entity =>
        {
            entity.HasKey(row => new { row.RequestId, row.RoleId });
            entity.HasOne(row => row.Request).WithMany(row => row.RequestedRoles).HasForeignKey(row => row.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(row => row.Role).WithMany().HasForeignKey(row => row.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DL.Entities.Notifications.ApplicationNotification>(entity =>
        {
            entity.HasIndex(row => new { row.RecipientSubject, row.EventKey }).IsUnique();
            entity.HasIndex(row => new { row.RecipientSubject, row.CreatedAt });
        });
        modelBuilder.Entity<DL.Entities.Notifications.NotificationDelivery>(entity =>
        {
            entity.HasIndex(row => new { row.DeliveredAt, row.NextAttemptAt });
            entity.HasOne(row => row.Notification).WithOne(row => row.Delivery).HasForeignKey<DL.Entities.Notifications.NotificationDelivery>(row => row.NotificationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CodeList>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.HasIndex(e => e.Kategorija);
            entity.HasQueryFilter(e => e.Aktivan);
            entity.HasOne(e => e.Definition).WithMany().HasForeignKey(e => e.DefinitionId).OnDelete(DeleteBehavior.Restrict);
            entity.Property<string>("NormalizedCategory").HasMaxLength(100).HasComputedColumnSql("UPPER(LTRIM(RTRIM([Kategorija])))", stored: true);
            entity.Property<string>("NormalizedCode").HasMaxLength(50).HasComputedColumnSql("UPPER(LTRIM(RTRIM([Kod])))", stored: true);
            entity.HasIndex("NormalizedCategory", "NormalizedCode").IsUnique().HasFilter(null);
            entity.Property(e => e.ResourceKey).UseCollation("Latin1_General_100_CI_AS");
            entity.HasIndex(e => new { e.Kategorija, e.LanguageCode, e.ResourceKey }).IsUnique()
                .HasFilter("[LanguageCode] IS NOT NULL AND [ResourceKey] IS NOT NULL");
        });
        modelBuilder.Entity<CodeListDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property<string>("NormalizedName").HasMaxLength(100).HasComputedColumnSql("UPPER(LTRIM(RTRIM([Name])))", stored: true);
            entity.HasIndex("NormalizedName").IsUnique().HasFilter(null);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeycloakId).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property<string>("NormalizedEmail").HasMaxLength(200)
                .HasComputedColumnSql("UPPER(LTRIM(RTRIM([Email])))", stored: true);
            entity.Property<string>("NormalizedUsername").HasMaxLength(100)
                .HasComputedColumnSql("UPPER(LTRIM(RTRIM([Username])))", stored: true);
            entity.HasIndex("NormalizedEmail").IsUnique().HasFilter(null);
            entity.HasIndex("NormalizedUsername").IsUnique().HasFilter(null);
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
            entity.Property<string>("NormalizedName").HasMaxLength(100).HasComputedColumnSql("UPPER(LTRIM(RTRIM([Name])))", stored: true);
            entity.HasIndex("NormalizedName").IsUnique().HasFilter(null);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<DL.Entities.Report.CapitalRevision>(entity =>
        {
            entity.ToTable("CapitalRevisions", table => table.UseSqlOutputClause(false));
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Revision).UseIdentityColumn(); entity.HasIndex(row => row.Revision).IsUnique();
            entity.Property(row => row.EffectiveDate).HasColumnType("date");
            entity.Property(row => row.RegulatoryCapital).HasPrecision(18, 2);
            entity.Property(row => row.CoreCapital).HasPrecision(18, 2);
            entity.Property(row => row.SupplementaryCapital).HasPrecision(18, 2);
            entity.HasIndex(row => new { row.EffectiveDate, row.Revision });
        });
        OnModelCreatingPartial(modelBuilder);
    }

    public DbSet<DL.Entities.Report.CapitalRevision> CapitalRevisions => Set<DL.Entities.Report.CapitalRevision>();

    private void ProtectCapitalRevisions()
    {
        if (ChangeTracker.Entries<DL.Entities.Report.CapitalRevision>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new Exceptions.BusinessRuleException(409, "capital.immutable_revision");
    }

    private void ProtectCreatorIdentity()
    {
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State == EntityState.Modified &&
                     (entry.Entity is DL.Entities.LegalEntity.LegalEntity || entry.Entity is DL.Entities.RelatedPersons.RelatedPerson)))
        {
            var creator = entry.Property("CreatorSubject");
            if (!Equals(creator.OriginalValue, creator.CurrentValue))
                throw new Exceptions.BusinessRuleException(409, "verification.creator_immutable");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectCreatorIdentity();
        ProtectCapitalRevisions();
        TouchCodebookVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProtectCreatorIdentity();
        ProtectCapitalRevisions();
        var categories = ChangedCodebookCategories();
        if (categories.Length > 0 || ChangeTracker.Entries<CodeListDefinition>().Any(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            var definitions = await CodeListDefinitions.Where(row => categories.Contains(row.Name.ToUpper())).ToListAsync(cancellationToken);
            TouchCodebookVersions(categories, definitions);
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private string[] ChangedCodebookCategories() => ChangeTracker.Entries<CodeList>()
        .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Select(entry => entry.Entity.Kategorija?.ToUpperInvariant()).OfType<string>().Distinct().ToArray();

    private void TouchCodebookVersions()
    {
        var categories = ChangedCodebookCategories();
        if (categories.Length == 0 && !ChangeTracker.Entries<CodeListDefinition>().Any(entry => entry.State is EntityState.Added or EntityState.Modified)) return;
        TouchCodebookVersions(categories, CodeListDefinitions.Where(row => categories.Contains(row.Name.ToUpper())).ToList());
    }

    private void TouchCodebookVersions(string[] categories, List<CodeListDefinition> definitions)
    {
        foreach (var row in CodeListDefinitions.Local.Where(row => categories.Contains(row.Name.ToUpper())).Concat(definitions).Distinct())
        {
            var entry = Entry(row);
            if (entry.State == EntityState.Added) row.Version = Math.Max(1, row.Version);
            else if (row.Version == entry.Property(item => item.Version).OriginalValue) row.Version++;
            row.ModifiedAt = DateTime.UtcNow;
        }
        foreach (var entry in ChangeTracker.Entries<CodeListDefinition>().Where(entry => entry.State == EntityState.Modified))
        {
            if (entry.Entity.Version == entry.Property(item => item.Version).OriginalValue) entry.Entity.Version++;
            entry.Entity.ModifiedAt = DateTime.UtcNow;
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
