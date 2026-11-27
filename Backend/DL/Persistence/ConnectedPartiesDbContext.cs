using System.Text.Json;
using System.Text.Json.Nodes;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.BL.Services;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext(
    DbContextOptions<ConnectedPartiesDbContext> options) : DbContext(options)
{

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

    private static void UseNumericSequence<TEntity>(ModelBuilder modelBuilder, string table) where TEntity : class
    {
        var sequence = table + "IdSequence";
        modelBuilder.HasSequence<long>(sequence).IncrementsBy(1);
        modelBuilder.Entity<TEntity>().Property<long>("Id").UseHiLo(sequence);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // These domains need permanent IDs before SaveChanges to build linked rows and audit entries.
        if (Database.IsSqlServer())
        {
            UseNumericSequence<AppUser>(modelBuilder, "AppUsers");
            UseNumericSequence<Role>(modelBuilder, "Roles");
            UseNumericSequence<Permission>(modelBuilder, "Permissions");
            UseNumericSequence<UserRole>(modelBuilder, "UserRoles");
            UseNumericSequence<DL.Entities.Authorization.RoleRequest>(modelBuilder, "RoleRequests");
            UseNumericSequence<DL.Entities.Notifications.ApplicationNotification>(modelBuilder, "Notifications");
            UseNumericSequence<DL.Entities.PeriodLock.UnlockRequest>(modelBuilder, "UnlockRequests");
            UseNumericSequence<DL.Entities.LegalEntity.LegalEntity>(modelBuilder, "LegalEntities");
            UseNumericSequence<DL.Entities.RelatedPersons.RelatedPerson>(modelBuilder, "RelatedPersons");
            UseNumericSequence<DL.Entities.RelatedPersons.FamilyMember>(modelBuilder, "FamilyMembers");
            UseNumericSequence<DL.Entities.Report.Report>(modelBuilder, "Reports");
        }

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
            entity.HasIndex(row => row.RequesterSubject).IsUnique().HasFilter("[STATUS] = N'pending'");
            entity.HasIndex(row => new { row.Status, row.CreatedAt });
        });
        modelBuilder.Entity<DL.Entities.Authorization.RequestedRole>(entity =>
        {
            entity.HasIndex(row => new { row.RequestId, row.RoleId }).IsUnique();
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
            entity.Property(e => e.Kod).IsRequired(false).HasColumnName("ID15").UseCollation("Latin1_General_100_CI_AS");
            entity.Property(e => e.Naziv).IsRequired(false).HasColumnName("ID17");
            entity.Property(e => e.Opis).HasColumnName("ID18");
            entity.Property(e => e.Aktivan).HasColumnName("IS_ACTIVE");
            entity.Property(e => e.KreiranDatum).HasColumnName("CreatedAt");
            entity.Property(e => e.KreiraoKorisnik).HasColumnName("CreatedBy");
            entity.Property(e => e.IzmijenjenDatum).HasColumnName("ModifiedAt");
            entity.Property(e => e.IzmijenioKorisnik).HasColumnName("ModifiedBy");
            entity.HasKey(e => e.ID);
            entity.HasQueryFilter(e => e.Aktivan);
            entity.Navigation(e => e.Definition).AutoInclude();
            entity.ToTable("CodeListValues");
            entity.HasOne(row => row.Definition).WithMany().HasForeignKey(row => row.DefinitionId).OnDelete(DeleteBehavior.Restrict);
            ConfigureCodeListSlots(entity);

        });
        modelBuilder.Entity<CodeListDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).UseCollation("Latin1_General_100_CI_AS");
            ConfigureDefinitionSlots(entity);
            entity.ToTable(table => table.HasCheckConstraint("CK_CodeListDefinitions_Name_Trimmed",
                "DATALENGTH([NAME]) = DATALENGTH(LTRIM(RTRIM([NAME])))"));
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeycloakId).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => new { e.LastName, e.FirstName, e.Id }).HasDatabaseName("IX_APP_USERS_LastNameFirstNameId");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).UseCollation("Latin1_General_100_CI_AS");
            entity.Property(e => e.Username).UseCollation("Latin1_General_100_CI_AS");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_AppUsers_Email_Trimmed",
                    "DATALENGTH([EMAIL]) = DATALENGTH(LTRIM(RTRIM([EMAIL])))");
                table.HasCheckConstraint("CK_AppUsers_Username_Trimmed",
                    "DATALENGTH([USERNAME]) = DATALENGTH(LTRIM(RTRIM([USERNAME])))");
            });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasQueryFilter(row => row.Action != RegistryVerification.CreationProof);
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Timestamp, e.Id }).IsDescending(true, true).HasDatabaseName("IX_AUDIT_LOGS_TimestampId");
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.TableName);
            entity.HasIndex(e => e.Username);
        });

        modelBuilder.Entity<RBBH.ConnectedParties.DL.Entities.Role.Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).UseCollation("Latin1_General_100_CI_AS");
            entity.ToTable(table => table.HasCheckConstraint("CK_Roles_Name_Trimmed",
                "DATALENGTH([NAME]) = DATALENGTH(LTRIM(RTRIM([NAME])))"));
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
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.Module, e.Code }).IsUnique().IsClustered()
                .HasDatabaseName("IX_PERMISSIONS_MODULE_CODE");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            entity.HasOne(e => e.Role)
                  .WithMany(role => role.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                  .WithMany(permission => permission.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DL.Entities.Report.Capital>(entity =>
        {
            entity.Property(row => row.CapitalDate).HasColumnType("date");
            entity.Property(row => row.ValidTill).HasColumnType("date");
            entity.HasIndex(row => row.IsActive).IsUnique().HasFilter("[IS_ACTIVE] = 1");
            entity.HasIndex(row => new { row.CapitalDate, row.ValidTill });
        });
        modelBuilder.Entity<Limit>().HasOne(row => row.Capital).WithMany().HasForeignKey(row => row.CapitalId).OnDelete(DeleteBehavior.Restrict);
        OnModelCreatingPartial(modelBuilder);
        ApplyDatabaseNames(modelBuilder);
    }

    private void ProtectCreatorIdentity()
    {
        if (ChangeTracker.Entries<AuditLog>().Any(entry =>
            entry.State is EntityState.Modified or EntityState.Deleted &&
            (entry.Entity.Action == RegistryVerification.CreationProof ||
             entry.Property(row => row.Action).OriginalValue == RegistryVerification.CreationProof)))
            throw new Exceptions.BusinessRuleException(409, "verification.creator_immutable");
    }

    private static readonly JsonSerializerOptions ReadableAuditJson = new() { WriteIndented = true };

    private void FormatNewAuditSnapshots()
    {
        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(entry => entry.State == EntityState.Added))
        {
            var audit = entry.Entity;
            var before = audit.OldValues;
            var after = audit.NewValues;
            if (audit.Action == "UPDATE" || audit.Action.EndsWith("_UPDATE", StringComparison.Ordinal))
                (before, after) = ChangedAuditSnapshots(before, after);
            audit.OldValues = FormatAuditSnapshot(before);
            audit.NewValues = FormatAuditSnapshot(after);
        }
    }

    private static (string? Before, string? After) ChangedAuditSnapshots(string? before, string? after)
    {
        if (before is null || after is null) return (before, after);
        try
        {
            if (JsonNode.Parse(before) is not JsonObject original ||
                JsonNode.Parse(after) is not JsonObject current)
                return (before, after);

            foreach (var key in original.Select(property => property.Key).ToArray())
            {
                if (!current.TryGetPropertyValue(key, out var value) ||
                    !JsonNode.DeepEquals(original[key], value)) continue;
                original.Remove(key);
                current.Remove(key);
            }
            return (original.Count == 0 ? null : original.ToJsonString(), current.Count == 0 ? null : current.ToJsonString());
        }
        catch (JsonException) { return (before, after); }
    }

    private static string? FormatAuditSnapshot(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot)) return snapshot;
        try
        {
            using var document = JsonDocument.Parse(snapshot);
            return JsonSerializer.Serialize(document.RootElement, ReadableAuditJson);
        }
        catch (JsonException)
        {
            return snapshot;
        }
    }

    private void ProtectCapitalHistory()
    {
        if (ChangeTracker.Entries<DL.Entities.Report.Capital>().Any(entry => entry.State == EntityState.Deleted ||
            entry.State == EntityState.Modified && !entry.OriginalValues.GetValue<bool>(nameof(DL.Entities.Report.Capital.IsActive))))
            throw new Exceptions.BusinessRuleException(409, "capital.immutable_revision");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectCreatorIdentity();
        ProtectCapitalHistory();
        FormatNewAuditSnapshots();
        TouchCodebookDefinitions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProtectCreatorIdentity();
        ProtectCapitalHistory();
        FormatNewAuditSnapshots();
        var categories = ChangedCodebookCategories();
        if (categories.Length > 0 || ChangeTracker.Entries<CodeListDefinition>().Any(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            List<CodeListDefinition> definitions = categories.Length == 0 ? [] :
                await CodeListDefinitions.Where(row => categories.Contains(row.Name.ToUpper())).ToListAsync(cancellationToken);
            TouchCodebookDefinitions(categories, definitions);
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private string[] ChangedCodebookCategories() => ChangeTracker.Entries<CodeList>()
        .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Select(entry => entry.Entity.Kategorija?.ToUpperInvariant()).OfType<string>().Distinct().ToArray();

    private void TouchCodebookDefinitions()
    {
        var categories = ChangedCodebookCategories();
        if (categories.Length == 0 && !ChangeTracker.Entries<CodeListDefinition>().Any(entry => entry.State is EntityState.Added or EntityState.Modified)) return;
        TouchCodebookDefinitions(categories,
            categories.Length == 0 ? [] : CodeListDefinitions.Where(row => categories.Contains(row.Name.ToUpper())).ToList());
    }

    private void TouchCodebookDefinitions(string[] categories, List<CodeListDefinition> definitions)
    {
        var knownDefinitions = CodeListDefinitions.Local.Where(row => categories.Contains(row.Name.ToUpperInvariant())).Concat(definitions)
            .Distinct().ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var category in ChangeTracker.Entries<CodeList>().Where(entry => entry.State == EntityState.Added)
                     .Select(entry => entry.Entity.Kategorija).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
        {
            if (!knownDefinitions.ContainsKey(category))
            {
                var definition = new CodeListDefinition { Name = category, CreatedBy = "system" };
                CodeListDefinitions.Add(definition);
                knownDefinitions.Add(category, definition);
            }
        }
        foreach (var entry in ChangeTracker.Entries<CodeList>().Where(item => item.State is EntityState.Added or EntityState.Modified).ToArray())
        {
            var value = entry.Entity;
            if (entry.State == EntityState.Added && value.Definition is null &&
                knownDefinitions.TryGetValue(value.Kategorija, out var definition))
                value.Definition = definition;
            if (value.Kategorija.Equals(LanguageBooks.Languages, StringComparison.OrdinalIgnoreCase) &&
                (entry.State == EntityState.Added || entry.Property(item => item.Naziv).IsModified))
            {
                value.Localization ??= new();
                value.Localization.LanguageCode = value.Kod;
                value.Localization.ResourceKey = value.Naziv;
            }
        }

        var touched = knownDefinitions.Values.Concat(ChangeTracker.Entries<CodeListDefinition>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified).Select(entry => entry.Entity));
        var now = DateTime.UtcNow;
        foreach (var definition in touched.Distinct().Where(row => Entry(row).State != EntityState.Deleted))
        {
            var entry = Entry(definition);
            if (entry.State == EntityState.Added && Enumerable.Range(1, 20).All(slot =>
                    entry.Property<string>($"ColumnName{slot}").CurrentValue is null))
                CodebookDefinitions.ConfigureReferenceFields(this, definition);
            var previous = entry.Property(row => row.ModifiedAt).OriginalValue;
            definition.ModifiedAt = previous is { } timestamp && timestamp >= now ? timestamp.AddTicks(1) : now;
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
