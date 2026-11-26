using RBBH.ConnectedParties.DL.Entities.Limiti;
using AppRole = RBBH.ConnectedParties.DL.Entities.Role.Role;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.DL.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(ConnectedPartiesDbContext db, CancellationToken ct = default)
    {
        if (await db.CodeLists.AnyAsync(ct))
        {
            await EnsureApplicationRolesAsync(db, ct);
            await EnsureReferenceCodeListsAsync(db, ct);
            return;
        }

        var now = DateTime.UtcNow;
        var physicalRole = new AppRole { Name = ApplicationAccessRoles.PhysicalPersons, CreatedBy = "seed" };
        var legalRole = new AppRole { Name = ApplicationAccessRoles.LegalPersons, CreatedBy = "seed" };
        var limitsRole = new AppRole { Name = ApplicationAccessRoles.Limits, CreatedBy = "seed" };
        var capitalRole = new AppRole { Name = ApplicationAccessRoles.Capital, CreatedBy = "seed" };
        var adminRole = new AppRole { Name = ApplicationAccessRoles.Administrator, CreatedBy = "seed" };
        var guestRole = new AppRole { Name = ApplicationAccessRoles.Guest, CreatedBy = "seed" };
        db.Roles.AddRange(physicalRole, legalRole, limitsRole, capitalRole, adminRole, guestRole);

        db.CodeLists.AddRange(
            Code("TIP_LICA", "FL", "Fizičko lice", now),
            Code("TIP_LICA", "PL", "Pravno lice", now),
            Code("OSNOV_POVEZANOSTI", "VL", "Vlasništvo", now),
            Code("OSNOV_POVEZANOSTI", "UP", "Upravljačka povezanost", now),
            Code("VRSTE_LIMITA", "MM", "MM", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "SATAFXs", "SATA FX spot", now, "Izvor: SATA. Iskorištenost FXs limita se po FBA ponderiše sa 1% — planirana i stvarna iskorištenost prikazuju se u ponderisanom iznosu od 1%."),
            Code("VRSTE_LIMITA", "SATAFXD", "SATA FX derivati", now, "Izvor: SATA."),
            Code("VRSTE_LIMITA", "SATAOVL", "SATA overnight limit", now, "Izvor: SATA."),
            Code("VRSTE_LIMITA", "OVL_CUSTODY", "OVL Custody", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "SFL_NET", "SFL Net", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "LG", "LG", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "LT", "LT", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "ST", "ST", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "SPRL", "SPRL", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "LEASING", "Leasing", now, "Izvor: manuelna evidencija."),
            Code("STATUS", "DRAFT", "Nacrt", now),
            Code("STATUS", "VERIFIED", "Verificirano", now),
            Code("STATUS", "REJECTED", "Odbijeno", now),
            Code("SRODSTVO", "1", "Bračni partner", now),
            Code("SRODSTVO", "2", "Partner", now),
            Code("SRODSTVO", "3", "Roditelj", now),
            Code("SRODSTVO", "4", "Dijete", now),
            Code("SRODSTVO", "5", "Brat ili sestra", now),
            Code("SRODSTVO", "6", "Očuh ili maćeha", now),
            Code("SRODSTVO", "7", "Pastorak", now),
            Code("SRODSTVO", "8", "Staratelj", now),
            Code("SRODSTVO", "99", "Drugo", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-U-2", "Zakon o bankama, član 2, paragraf u, odjeljak 2", now, "Lice sa najmanje 5% učešća u banci ili članu bankarske grupe i članovi njegove uže porodice."),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-1", "Zakon o bankama, član 2, paragraf v, odjeljak 1", now, "Član bankarske grupe u kojoj je banka."),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-3", "Zakon o bankama, član 2, paragraf v, odjeljak 3", now, "Pravno lice u kojem banka ima kvalifikovano učešće."),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-4", "Zakon o bankama, član 2, paragraf v, odjeljak 4", now, "Pravno lice povezano preko člana organa banke ili njegove uže porodice."),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-5", "Zakon o bankama, član 2, paragraf v, odjeljak 5", now, "Član organa banke, nosilac ključne funkcije, prokurista ili član uže porodice."),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-7", "Zakon o bankama, član 2, paragraf v, odjeljak 7", now, "Član organa člana bankarske grupe ili član njegove uže porodice."),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-8", "Zakon o bankama, član 2, paragraf v, odjeljak 8", now, "Lice sa značajnim uticajem na poslovanje banke ili sukobom interesa."),
            Code("OSNOV_POSEBNOG_ODNOSA", "NADZORNI_ODBOR", "Član nadzornog odbora banke", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "UPRAVA", "Član Uprave Banke", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "NKF", "Nosilac ključne funkcije", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "PROKURISTA", "Prokurista banke", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "B1", "B1", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "UZA_PORODICA", "Član uže porodice povezanog lica", now));

        var resident = new RelatedPerson { FirstName = "Amina", LastName = "Hadžić", Residency = ResidencyType.Resident, JMBG = "0101990170003", RelationBasis = "ZOB-2-V-5", SpecialRelationBasis = "UPRAVA", IsIdentifiedStaff = true, DeclarationNoFamilyMembers = false, ConnectedWithBank = true, SpecialRelationshipWithBank = true, SpecialContract = false, MalusClawback = true, DateFrom = now.Date.AddYears(-2), DateTo = now.Date.AddYears(1), Status = RelatedPersonStatus.Verified, CreatedBy = "seed" };
        var nonResident = new RelatedPerson { FirstName = "Marko", LastName = "Kovač", Residency = ResidencyType.NonResident, PassportNumber = "P-DEMO-2026", FBAId = "1002", RelationBasis = "ZOB-2-V-8", SpecialRelationBasis = "PROKURISTA", IsIdentifiedStaff = true, DeclarationNoFamilyMembers = false, ConnectedWithBank = false, SpecialRelationshipWithBank = true, SpecialContract = true, MalusClawback = false, DateFrom = now.Date.AddYears(-1), DateTo = now.Date.AddYears(1), Status = RelatedPersonStatus.Draft, CreatedBy = "seed" };
        var rejected = new RelatedPerson { FirstName = "Lejla", LastName = "Testić", Residency = ResidencyType.NonResident, PassportNumber = "P-REJECT-01", FBAId = "1003", RelationBasis = "ZOB-2-V-7", SpecialRelationBasis = "NKF", IsIdentifiedStaff = true, DeclarationNoFamilyMembers = false, ConnectedWithBank = false, SpecialRelationshipWithBank = false, SpecialContract = false, MalusClawback = false, DateFrom = now.Date.AddMonths(-6), DateTo = now.Date.AddMonths(6), Status = RelatedPersonStatus.Rejected, CreatedBy = "seed" };
        db.RelatedPersons.AddRange(resident, nonResident, rejected);
        var spouse = new RelatedPerson { FirstName = "Emir", LastName = "Hadžić", Residency = ResidencyType.Resident, JMBG = "0202990170004", RelationBasis = "ZOB-2-V-5", SpecialRelationBasis = "UZA_PORODICA", IsIdentifiedStaff = false, DeclarationNoFamilyMembers = true, ConnectedWithBank = true, SpecialRelationshipWithBank = false, SpecialContract = false, MalusClawback = false, DateFrom = now.Date.AddYears(-2), DateTo = now.Date.AddYears(1), RelatedToPersonId = resident.Id, Status = RelatedPersonStatus.Verified, CreatedBy = "seed" };
        var child = new RelatedPerson { FirstName = "Ana", LastName = "Kovač", Residency = ResidencyType.NonResident, PassportNumber = "P-FAMILY-01", FBAId = "1004", RelationBasis = "ZOB-2-V-5", SpecialRelationBasis = "UZA_PORODICA", IsIdentifiedStaff = false, DeclarationNoFamilyMembers = true, ConnectedWithBank = true, SpecialRelationshipWithBank = false, SpecialContract = false, MalusClawback = false, DateFrom = now.Date.AddYears(-1), DateTo = now.Date.AddYears(1), RelatedToPersonId = nonResident.Id, Status = RelatedPersonStatus.Draft, CreatedBy = "seed" };
        db.RelatedPersons.AddRange(spouse, child);

        var legalResident = new LegalEntity { IsResident = true, TaxNumber = "4200000000001", Name = "RBI Poslovni partner d.o.o.", BasisOfConnection = "Član 4. stav (1) tačka a) ZOB", ConnectedWithBank = true, DateFrom = now.Date.AddYears(-3), Status = "VERIFIED", CreatedBy = "seed", VerifiedBy = "demo.verifier", VerifiedAt = now.AddDays(-2) };
        var legalForeign = new LegalEntity { IsResident = false, FbaId = "2002", Name = "International Partner GmbH", BasisOfConnection = "Član 4. stav (1) tačka b) ZOB", ConnectedWithBank = false, DateFrom = now.Date.AddMonths(-8), Status = "DRAFT", CreatedBy = "seed" };
        db.Set<LegalEntity>().AddRange(legalResident, legalForeign);

        db.Limiti.AddRange(
            new Limit { LegalEntityId = legalResident.Id, TipLimita = "MM", OdobreniLimit = 1_000_000, MaksimalnaOcekivanaUtilizacija = 750_000, RokOcekivaneUtilizacije = now.Date.AddMonths(6), Komentar = "Testni limit za rezidentno pravno lice.", CreatedBy = "seed" },
            new Limit { LegalEntityId = legalForeign.Id, TipLimita = "SATAFXS", OdobreniLimit = 500_000, MaksimalnaOcekivanaUtilizacija = 300_000, RokOcekivaneUtilizacije = now.Date.AddMonths(3), Komentar = "Testni limit za nerezidentno pravno lice.", CreatedBy = "seed" });

        var admin = new AppUser { KeycloakSubjectConfirmed = true, KeycloakId = "local-admin", Username = "admin1", FirstName = "Lokalni", LastName = "Korisnik", Email = "admin@localhost", CreatedBy = "seed" };
        var verifier = new AppUser { KeycloakSubjectConfirmed = true, KeycloakId = "local-multi-access", Username = "user1", FirstName = "Vera", LastName = "Korisnik", Email = "user@localhost", CreatedBy = "seed" };
        var inactive = new AppUser { KeycloakSubjectConfirmed = true, KeycloakId = "local-inactive", Username = "inactive1", FirstName = "Neaktivni", LastName = "Korisnik", Email = "inactive@localhost", IsActive = false, CreatedBy = "seed" };
        db.AppUsers.AddRange(admin, verifier, inactive);
        db.UserRoles.AddRange(
            new UserRole { UserId = admin.Id, RoleId = physicalRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = legalRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = limitsRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = capitalRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = verifier.Id, RoleId = legalRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = verifier.Id, RoleId = limitsRole.Id, CreatedBy = "seed" });

        db.PeriodLocks.AddRange(
            new PeriodLock { Year = now.Year, Month = now.Month, IsLocked = false, CreatedBy = "seed" },
            new PeriodLock { Year = now.AddMonths(-1).Year, Month = now.AddMonths(-1).Month, IsLocked = true, LockedBy = admin.Username, LockedAt = now.AddDays(-10), CreatedBy = "seed" });
        db.UnlockRequests.AddRange(
            new UnlockRequest { RequestedBy = verifier.Username, RequestedByEmail = verifier.Email, Year = now.Year, Month = now.Month, Reason = "Potrebna je korekcija testnih podataka za izvještaj.", Status = "PENDING", CreatedBy = "seed" },
            new UnlockRequest { RequestedBy = "user1", RequestedByEmail = "user@localhost", Year = now.AddMonths(-1).Year, Month = now.AddMonths(-1).Month, Reason = "Naknadno dostavljena dokumentacija zahtijeva korekciju.", Status = "REJECTED", AdminNote = "Dokumentacija nije potpuna.", ProcessedBy = admin.Username, ProcessedAt = now.AddDays(-3), CreatedBy = "seed" });

        db.Reports.AddRange(
            new Report { ReportType = "DAILY", ReportDate = now.Date, TotalClients = 4, ClientsWithBreachedLimit = 1, TotalExposure = 550000, CreatedBy = "seed" },
            new Report { ReportType = "MONTHLY", ReportDate = new DateTime(now.Year, now.Month, 1), TotalClients = 4, ClientsWithBreachedLimit = 1, TotalExposure = 550000, CreatedBy = "seed" });
        var capital = new Capital
        {
            RegulatoryCapital = 5000000, CoreCapital = 4000000, SupplementaryCapital = 500000,
            CapitalDate = now.Date, CreatedBy = "seed"
        };
        db.Capitals.Add(capital);
        foreach (var limit in db.Limiti.Local) limit.Capital = capital;

        db.AuditLogs.AddRange(
            new AuditLog { TableName = "RelatedPerson", Action = "VERIFY", NewValues = "{\"status\":\"Verified\",\"verifiedBy\":\"demo.verifier\"}", Username = verifier.Username, Timestamp = now.AddHours(-2) },
            new AuditLog { TableName = "LegalEntity", Action = "INSERT", NewValues = "{\"name\":\"International Partner GmbH\",\"status\":\"Draft\"}", Username = admin.Username, Timestamp = now.AddDays(-1) },
            new AuditLog { TableName = "PeriodLock", Action = "PERIOD_UNLOCK", NewValues = "{\"isLocked\":false}", Username = admin.Username, Timestamp = now.AddDays(-2) });

        await db.SaveChangesAsync(ct);
        await EnsureApplicationRolesAsync(db, ct);
    }

    private static CodeList Code(string category, string code, string name, DateTime now, string? description = null) =>
        new() { Kategorija = category, Kod = code.Trim().ToUpper(), Naziv = name, Opis = description, Aktivan = true, KreiranDatum = now, KreiraoKorisnik = "seed" };

    private static async Task EnsureReferenceCodeListsAsync(ConnectedPartiesDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var required = new[]
        {
            Code("VRSTE_LIMITA", "MM", "MM", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "SATAFXs", "SATA FX spot", now, "Izvor: SATA. Iskorištenost FXs limita se po FBA ponderiše sa 1% — planirana i stvarna iskorištenost prikazuju se u ponderisanom iznosu od 1%."),
            Code("VRSTE_LIMITA", "SATAFXD", "SATA FX derivati", now, "Izvor: SATA."),
            Code("VRSTE_LIMITA", "SATAOVL", "SATA overnight limit", now, "Izvor: SATA."),
            Code("VRSTE_LIMITA", "OVL_CUSTODY", "OVL Custody", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "SFL_NET", "SFL Net", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "LG", "LG", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "LT", "LT", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "ST", "ST", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "SPRL", "SPRL", now, "Izvor: manuelna evidencija."),
            Code("VRSTE_LIMITA", "LEASING", "Leasing", now, "Izvor: manuelna evidencija."),
            Code("SRODSTVO", "1", "Bračni partner", now), Code("SRODSTVO", "2", "Partner", now), Code("SRODSTVO", "3", "Roditelj", now),
            Code("SRODSTVO", "4", "Dijete", now), Code("SRODSTVO", "5", "Brat ili sestra", now), Code("SRODSTVO", "6", "Očuh ili maćeha", now),
            Code("SRODSTVO", "7", "Pastorak", now), Code("SRODSTVO", "8", "Staratelj", now), Code("SRODSTVO", "99", "Drugo", now),
            Code("OSNOV_POVEZANOSTI", "VL", "Vlasništvo", now), Code("OSNOV_POVEZANOSTI", "UP", "Upravljačka povezanost", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-U-2", "Zakon o bankama, član 2, paragraf u, odjeljak 2", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-1", "Zakon o bankama, član 2, paragraf v, odjeljak 1", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-3", "Zakon o bankama, član 2, paragraf v, odjeljak 3", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-4", "Zakon o bankama, član 2, paragraf v, odjeljak 4", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-5", "Zakon o bankama, član 2, paragraf v, odjeljak 5", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-7", "Zakon o bankama, član 2, paragraf v, odjeljak 7", now),
            Code("OSNOV_POVEZANOSTI", "ZOB-2-V-8", "Zakon o bankama, član 2, paragraf v, odjeljak 8", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "NADZORNI_ODBOR", "Član nadzornog odbora banke", now), Code("OSNOV_POSEBNOG_ODNOSA", "UPRAVA", "Član Uprave Banke", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "NKF", "Nosilac ključne funkcije", now), Code("OSNOV_POSEBNOG_ODNOSA", "PROKURISTA", "Prokurista banke", now),
            Code("OSNOV_POSEBNOG_ODNOSA", "B1", "B1", now), Code("OSNOV_POSEBNOG_ODNOSA", "UZA_PORODICA", "Član uže porodice povezanog lica", now)
        };
        var existing = await db.CodeLists.IgnoreQueryFilters().Select(item => item.Definition!.Name + "|" + item.Kod).ToListAsync(ct);
        db.CodeLists.AddRange(required.Where(item => !existing.Contains(item.Kategorija + "|" + item.Kod)));
        await db.SaveChangesAsync(ct);
    }

    public static async Task EnsureApplicationRolesAsync(ConnectedPartiesDbContext db, CancellationToken ct = default)
    {
        var roles = await db.Roles.AsTracking().ToListAsync(ct);
        var legacyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["physical-persons"] = ApplicationAccessRoles.PhysicalPersons,
            ["legal-persons"] = ApplicationAccessRoles.LegalPersons,
            ["limits"] = ApplicationAccessRoles.Limits,
            ["regulatory-reporting"] = ApplicationAccessRoles.Capital
        };
        foreach (var role in roles.Where(role => legacyNames.ContainsKey(role.Name)))
            role.Name = legacyNames[role.Name];

        foreach (var role in roles.Where(role => !ApplicationAccessRoles.All.Contains(role.Name)))
            role.IsActive = false;
        foreach (var name in ApplicationAccessRoles.All.Where(name => roles.All(role => !role.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
        {
            var role = new AppRole { Name = name, CreatedBy = "seed" };
            db.Roles.Add(role);
            roles.Add(role);
        }
        var localAdmin = await db.AppUsers.FirstOrDefaultAsync(user => user.Username == "admin1", ct);
        if (localAdmin is not null)
        {
            var assignments = await db.UserRoles.AsTracking()
                .Where(item => item.UserId == localAdmin.Id)
                .ToListAsync(ct);
            foreach (var role in roles.Where(role => role.Name == ApplicationAccessRoles.Administrator))
            {
                var existing = assignments.FirstOrDefault(item => item.RoleId == role.Id);
                if (existing is null)
                    db.UserRoles.Add(new UserRole { UserId = localAdmin.Id, RoleId = role.Id, CreatedBy = "seed" });
                else
                    existing.SetActive(true, "seed", DateTime.UtcNow);
            }
        }

        var moduleRoleIds = roles
            .Where(role => ApplicationAccessRoles.BusinessModules.Contains(role.Name))
            .Select(role => role.Id)
            .ToArray();
        var adminRole = roles.First(role => role.Name == ApplicationAccessRoles.Administrator);
        var legacyAdministrators = await db.UserRoles
            .Where(userRole => userRole.IsActive && moduleRoleIds.Contains(userRole.RoleId))
            .GroupBy(userRole => userRole.UserId)
            .Where(group => group.Select(userRole => userRole.RoleId).Distinct().Count() == moduleRoleIds.Length)
            .Select(group => group.Key)
            .ToListAsync(ct);
        var assignedAdminIds = await db.UserRoles
            .Where(userRole => userRole.RoleId == adminRole.Id)
            .Select(userRole => userRole.UserId)
            .ToListAsync(ct);
        foreach (var userId in legacyAdministrators.Where(userId => !assignedAdminIds.Contains(userId)))
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = adminRole.Id, CreatedBy = "role-migration" });

        await db.SaveChangesAsync(ct);

        var permissions = await db.Permissions.AsTracking().ToListAsync(ct);
        foreach (var code in ApplicationPermissions.All)
        {
            var permission = permissions.FirstOrDefault(item =>
                item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (permission is null)
            {
                permission = new Permission
                {
                    Code = code,
                    Module = code[..code.LastIndexOf('_')]
                };
                db.Permissions.Add(permission);
                permissions.Add(permission);
            }
            else
                permission.IsActive = true;
        }

        foreach (var permission in permissions.Where(item => !ApplicationPermissions.All.Contains(item.Code)))
            permission.IsActive = false;

        var existingMappings = await db.RolePermissions.AsTracking().ToListAsync(ct);
        var expectedMappings = RolePermissionMatrix.PermissionsByRole
            .SelectMany(pair => pair.Value.Select(permissionCode => new
            {
                RoleId = roles.Single(role => role.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)).Id,
                PermissionId = permissions.Single(permission => permission.Code.Equals(permissionCode, StringComparison.OrdinalIgnoreCase)).Id
            }))
            .ToArray();

        foreach (var mapping in expectedMappings.Where(mapping => existingMappings.All(existing =>
                     existing.RoleId != mapping.RoleId || existing.PermissionId != mapping.PermissionId)))
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = mapping.RoleId,
                PermissionId = mapping.PermissionId
            });

        db.RolePermissions.RemoveRange(existingMappings.Where(existing => expectedMappings.All(expected =>
            expected.RoleId != existing.RoleId || expected.PermissionId != existing.PermissionId)));

        await db.SaveChangesAsync(ct);
    }
}
