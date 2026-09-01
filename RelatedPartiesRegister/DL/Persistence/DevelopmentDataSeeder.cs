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
        var reportingRole = new AppRole { Name = ApplicationAccessRoles.RegulatoryReporting, CreatedBy = "seed" };
        db.Roles.AddRange(physicalRole, legalRole, limitsRole, reportingRole);

        db.CodeLists.AddRange(
            Code("TipLica", "FL", "Fizičko lice", 1, now),
            Code("TipLica", "PL", "Pravno lice", 2, now),
            Code("OsnovPovezanosti", "VL", "Vlasništvo", 1, now),
            Code("OsnovPovezanosti", "UP", "Upravljačka povezanost", 2, now),
            Code("VrstaLimita", "REG", "Regulatorni limit", 1, now),
            Code("VrstaLimita", "INT", "Interni limit", 2, now),
            Code("Status", "DRAFT", "Nacrt", 1, now),
            Code("Status", "VERIFIED", "Verificirano", 2, now),
            Code("Status", "REJECTED", "Odbijeno", 3, now),
            Code("Srodstvo", "1", "Bračni partner", 1, now),
            Code("Srodstvo", "2", "Partner", 2, now),
            Code("Srodstvo", "3", "Roditelj", 3, now),
            Code("Srodstvo", "4", "Dijete", 4, now),
            Code("Srodstvo", "5", "Brat ili sestra", 5, now),
            Code("Srodstvo", "6", "Očuh ili maćeha", 6, now),
            Code("Srodstvo", "7", "Pastorak", 7, now),
            Code("Srodstvo", "8", "Staratelj", 8, now),
            Code("Srodstvo", "99", "Drugo", 9, now),
            Code("OsnovPovezanosti", "ZOB-2-U-2", "Zakon o bankama, član 2, paragraf u, odjeljak 2", 10, now, "Lice sa najmanje 5% učešća u banci ili članu bankarske grupe i članovi njegove uže porodice."),
            Code("OsnovPovezanosti", "ZOB-2-V-1", "Zakon o bankama, član 2, paragraf v, odjeljak 1", 11, now, "Član bankarske grupe u kojoj je banka."),
            Code("OsnovPovezanosti", "ZOB-2-V-3", "Zakon o bankama, član 2, paragraf v, odjeljak 3", 12, now, "Pravno lice u kojem banka ima kvalifikovano učešće."),
            Code("OsnovPovezanosti", "ZOB-2-V-4", "Zakon o bankama, član 2, paragraf v, odjeljak 4", 13, now, "Pravno lice povezano preko člana organa banke ili njegove uže porodice."),
            Code("OsnovPovezanosti", "ZOB-2-V-5", "Zakon o bankama, član 2, paragraf v, odjeljak 5", 14, now, "Član organa banke, nosilac ključne funkcije, prokurista ili član uže porodice."),
            Code("OsnovPovezanosti", "ZOB-2-V-7", "Zakon o bankama, član 2, paragraf v, odjeljak 7", 15, now, "Član organa člana bankarske grupe ili član njegove uže porodice."),
            Code("OsnovPovezanosti", "ZOB-2-V-8", "Zakon o bankama, član 2, paragraf v, odjeljak 8", 16, now, "Lice sa značajnim uticajem na poslovanje banke ili sukobom interesa."),
            Code("OsnovPosebnogOdnosa", "NADZORNI_ODBOR", "Član nadzornog odbora banke", 1, now),
            Code("OsnovPosebnogOdnosa", "UPRAVA", "Član Uprave Banke", 2, now),
            Code("OsnovPosebnogOdnosa", "NKF", "Nosilac ključne funkcije", 3, now),
            Code("OsnovPosebnogOdnosa", "PROKURISTA", "Prokurista banke", 4, now),
            Code("OsnovPosebnogOdnosa", "B1", "B1", 5, now),
            Code("OsnovPosebnogOdnosa", "UZA_PORODICA", "Član uže porodice povezanog lica", 6, now));

        db.Limiti.AddRange(
            new Limit { Naziv = "Ukupna izloženost", TipLimita = "REG", IznosLimita = 1_000_000, Utilizacija = 425_000, RaspoloziviLimit = 575_000, RegulatorniKapital = 5_000_000, OsnovniKapital = 4_000_000, CreatedBy = "seed" },
            new Limit { Naziv = "Interni operativni limit", TipLimita = "INT", IznosLimita = 500_000, Utilizacija = 125_000, RaspoloziviLimit = 375_000, RegulatorniKapital = 5_000_000, OsnovniKapital = 4_000_000, CreatedBy = "seed" });

        var resident = new RelatedPerson { FirstName = "Amina", LastName = "Hadžić", Residency = ResidencyType.Resident, JMBG = "0101990170003", GCCNumber = "1001", GCCName = "RBI GCC", RelationBasis = "ZOB-2-V-5", RelationDescription = "Član uprave banke i članovi uže porodice.", SpecialRelationBasis = "UPRAVA", IsIdentifiedStaff = true, DeclarationNoFamilyMembers = false, ConnectedWithBank = true, SpecialRelationshipWithBank = true, SpecialContract = false, MalusClawback = true, DateFrom = now.Date.AddYears(-2), DateTo = now.Date.AddYears(1), Status = RelatedPersonStatus.Verified, CreatedBy = "seed" };
        var nonResident = new RelatedPerson { FirstName = "Marko", LastName = "Kovač", Residency = ResidencyType.NonResident, PassportNumber = "P-DEMO-2026", FBAId = "1002", GCCNumber = "1002", GCCName = "Međunarodni GCC", RelationBasis = "ZOB-2-V-8", RelationDescription = "Lice sa značajnim uticajem ili mogućim sukobom interesa.", SpecialRelationBasis = "PROKURISTA", IsIdentifiedStaff = true, DeclarationNoFamilyMembers = false, ConnectedWithBank = false, SpecialRelationshipWithBank = true, SpecialContract = true, MalusClawback = false, DateFrom = now.Date.AddYears(-1), DateTo = now.Date.AddYears(1), Status = RelatedPersonStatus.Draft, CreatedBy = "seed" };
        var rejected = new RelatedPerson { FirstName = "Lejla", LastName = "Testić", Residency = ResidencyType.NonResident, PassportNumber = "P-REJECT-01", FBAId = "1003", GCCNumber = "1003", GCCName = "Test GCC", RelationBasis = "ZOB-2-V-7", RelationDescription = "Član organa upravljanja člana bankarske grupe.", SpecialRelationBasis = "NKF", IsIdentifiedStaff = true, DeclarationNoFamilyMembers = false, ConnectedWithBank = false, SpecialRelationshipWithBank = false, SpecialContract = false, MalusClawback = false, DateFrom = now.Date.AddMonths(-6), DateTo = now.Date.AddMonths(6), Status = RelatedPersonStatus.Rejected, CreatedBy = "seed" };
        var spouse = new RelatedPerson { FirstName = "Emir", LastName = "Hadžić", Residency = ResidencyType.Resident, JMBG = "0202990170004", GCCNumber = "1001", GCCName = "RBI GCC", RelationBasis = "ZOB-2-V-5", RelationDescription = "Član uže porodice povezanog lica.", SpecialRelationBasis = "UZA_PORODICA", IsIdentifiedStaff = false, DeclarationNoFamilyMembers = true, ConnectedWithBank = true, SpecialRelationshipWithBank = false, SpecialContract = false, MalusClawback = false, DateFrom = now.Date.AddYears(-2), DateTo = now.Date.AddYears(1), RelatedToPersonId = resident.Id, FamilyRelationshipType = FamilyRelationshipType.Spouse, Status = RelatedPersonStatus.Verified, CreatedBy = "seed" };
        var child = new RelatedPerson { FirstName = "Ana", LastName = "Kovač", Residency = ResidencyType.NonResident, PassportNumber = "P-FAMILY-01", FBAId = "1004", GCCNumber = "1002", GCCName = "Međunarodni GCC", RelationBasis = "ZOB-2-V-5", RelationDescription = "Član uže porodice povezanog lica.", SpecialRelationBasis = "UZA_PORODICA", IsIdentifiedStaff = false, DeclarationNoFamilyMembers = true, ConnectedWithBank = true, SpecialRelationshipWithBank = false, SpecialContract = false, MalusClawback = false, DateFrom = now.Date.AddYears(-1), DateTo = now.Date.AddYears(1), RelatedToPersonId = nonResident.Id, FamilyRelationshipType = FamilyRelationshipType.Child, Status = RelatedPersonStatus.Draft, CreatedBy = "seed" };
        db.RelatedPersons.AddRange(resident, nonResident, rejected, spouse, child);

        var legalResident = new LegalEntity { IsResident = true, TaxNumber = "4200000000001", Name = "RBI Poslovni partner d.o.o.", GccNumber = "2001", GccName = "RBI GCC", BasisOfConnection = "Član 4. stav (1) tačka a) ZOB", ConnectionDescription = "Član 4. stav (1) tačka a) ZOB", ConnectedWithBank = true, DateFrom = now.Date.AddYears(-3), Status = "VERIFIED", CreatedBy = "seed", VerifiedBy = "demo.verifier", VerifiedAt = now.AddDays(-2) };
        var legalForeign = new LegalEntity { IsResident = false, FbaId = "2002", Name = "International Partner GmbH", GccNumber = "2002", GccName = "International GCC", BasisOfConnection = "Član 4. stav (1) tačka b) ZOB", ConnectionDescription = "Član 4. stav (1) tačka b) ZOB", ConnectedWithBank = false, DateFrom = now.Date.AddMonths(-8), Status = "DRAFT", CreatedBy = "seed" };
        db.Set<LegalEntity>().AddRange(legalResident, legalForeign);

        var admin = new AppUser { KeycloakId = "local-admin", Username = "admin1", FirstName = "Lokalni", LastName = "Korisnik", Email = "admin@localhost", CreatedBy = "seed" };
        var verifier = new AppUser { KeycloakId = "local-multi-access", Username = "user1", FirstName = "Vera", LastName = "Korisnik", Email = "user@localhost", CreatedBy = "seed" };
        var inactive = new AppUser { KeycloakId = "local-inactive", Username = "inactive1", FirstName = "Neaktivni", LastName = "Korisnik", Email = "inactive@localhost", IsActive = false, CreatedBy = "seed" };
        db.AppUsers.AddRange(admin, verifier, inactive);
        db.UserRoles.AddRange(
            new UserRole { UserId = admin.Id, RoleId = physicalRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = legalRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = limitsRole.Id, CreatedBy = "seed" },
            new UserRole { UserId = admin.Id, RoleId = reportingRole.Id, CreatedBy = "seed" },
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
        db.ClientLimits.AddRange(
            new ClientLimit { LegalEntityId = legalResident.Id, RegulatoryCapital = 5000000, CoreCapital = 4000000, ExposureLimit = 1000000, CurrentExposure = 425000, Currency = "BAM", CreatedBy = "seed" },
            new ClientLimit { LegalEntityId = legalForeign.Id, RegulatoryCapital = 5000000, CoreCapital = 4000000, ExposureLimit = 100000, CurrentExposure = 125000, Currency = "EUR", IsLimitBreached = true, CreatedBy = "seed" });

        db.AuditLogs.AddRange(
            new AuditLog { TableName = "RelatedPerson", RecordId = resident.Id.ToString(), Action = "VERIFY", NewValues = "{\"status\":\"Verified\",\"verifiedBy\":\"demo.verifier\"}", UserId = verifier.Username, Username = verifier.Username, IpAddress = "127.0.0.1", Timestamp = now.AddHours(-2) },
            new AuditLog { TableName = "LegalEntity", RecordId = legalForeign.Id.ToString(), Action = "INSERT", NewValues = "{\"name\":\"International Partner GmbH\",\"status\":\"Draft\"}", UserId = admin.Username, Username = admin.Username, IpAddress = "127.0.0.1", Timestamp = now.AddDays(-1) },
            new AuditLog { TableName = "PeriodLock", RecordId = $"{now.Year}-{now.Month}", Action = "PERIOD_UNLOCK", NewValues = "{\"isLocked\":false}", UserId = admin.Username, Username = admin.Username, IpAddress = "127.0.0.1", Timestamp = now.AddDays(-2) });

        await db.SaveChangesAsync(ct);
    }

    private static CodeList Code(string category, string code, string name, int order, DateTime now, string? description = null) =>
        new() { Kategorija = category, Kod = code, Naziv = name, Opis = description, RedoslijedPrikaza = order, Aktivan = true, KreiranDatum = now, KreiraoKorisnik = "seed" };

    private static async Task EnsureReferenceCodeListsAsync(ConnectedPartiesDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var required = new[]
        {
            Code("VrstaLimita", "REG", "Regulatorni limit", 1, now), Code("VrstaLimita", "INT", "Interni limit", 2, now),
            Code("Srodstvo", "1", "Bračni partner", 1, now), Code("Srodstvo", "2", "Partner", 2, now), Code("Srodstvo", "3", "Roditelj", 3, now),
            Code("Srodstvo", "4", "Dijete", 4, now), Code("Srodstvo", "5", "Brat ili sestra", 5, now), Code("Srodstvo", "6", "Očuh ili maćeha", 6, now),
            Code("Srodstvo", "7", "Pastorak", 7, now), Code("Srodstvo", "8", "Staratelj", 8, now), Code("Srodstvo", "99", "Drugo", 9, now),
            Code("OsnovPovezanosti", "VL", "Vlasništvo", 1, now), Code("OsnovPovezanosti", "UP", "Upravljačka povezanost", 2, now),
            Code("OsnovPovezanosti", "ZOB-2-U-2", "Zakon o bankama, član 2, paragraf u, odjeljak 2", 10, now),
            Code("OsnovPovezanosti", "ZOB-2-V-1", "Zakon o bankama, član 2, paragraf v, odjeljak 1", 11, now),
            Code("OsnovPovezanosti", "ZOB-2-V-3", "Zakon o bankama, član 2, paragraf v, odjeljak 3", 12, now),
            Code("OsnovPovezanosti", "ZOB-2-V-4", "Zakon o bankama, član 2, paragraf v, odjeljak 4", 13, now),
            Code("OsnovPovezanosti", "ZOB-2-V-5", "Zakon o bankama, član 2, paragraf v, odjeljak 5", 14, now),
            Code("OsnovPovezanosti", "ZOB-2-V-7", "Zakon o bankama, član 2, paragraf v, odjeljak 7", 15, now),
            Code("OsnovPovezanosti", "ZOB-2-V-8", "Zakon o bankama, član 2, paragraf v, odjeljak 8", 16, now),
            Code("OsnovPosebnogOdnosa", "NADZORNI_ODBOR", "Član nadzornog odbora banke", 1, now), Code("OsnovPosebnogOdnosa", "UPRAVA", "Član Uprave Banke", 2, now),
            Code("OsnovPosebnogOdnosa", "NKF", "Nosilac ključne funkcije", 3, now), Code("OsnovPosebnogOdnosa", "PROKURISTA", "Prokurista banke", 4, now),
            Code("OsnovPosebnogOdnosa", "B1", "B1", 5, now), Code("OsnovPosebnogOdnosa", "UZA_PORODICA", "Član uže porodice povezanog lica", 6, now)
        };
        var existing = await db.CodeLists.IgnoreQueryFilters().Select(item => item.Kategorija + "|" + item.Kod).ToListAsync(ct);
        db.CodeLists.AddRange(required.Where(item => !existing.Contains(item.Kategorija + "|" + item.Kod)));
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureApplicationRolesAsync(ConnectedPartiesDbContext db, CancellationToken ct)
    {
        var roles = await db.Roles.AsTracking().ToListAsync(ct);
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
            foreach (var role in roles.Where(role => ApplicationAccessRoles.All.Contains(role.Name)))
            {
                var existing = assignments.FirstOrDefault(item => item.RoleId == role.Id);
                if (existing is null)
                    db.UserRoles.Add(new UserRole { UserId = localAdmin.Id, RoleId = role.Id, CreatedBy = "seed" });
                else
                    existing.IsActive = true;
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
