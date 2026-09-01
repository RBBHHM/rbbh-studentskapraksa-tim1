using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntegrationTests.Tests.PeriodLock
{
    /// <summary>
    /// Integracijski testovi zaključavanja perioda, zahtjeva i audit zapisa.
    ///
    /// Autorizacijski atributi provjeravaju se refleksijom, a poslovna logika i audit
    /// nad stvarnim testnim kontekstom.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class PeriodLockEdgeCaseTests
    {
        private readonly DatabaseFixture _fixture;
        private const string AdminUser = "admin.test";
        private const string RegularUser = "korisnik.test";

        public PeriodLockEdgeCaseTests(DatabaseFixture fixture) => _fixture = fixture;

        private PeriodLockController NewAdminController()
        {
            var ctx = _fixture.CreateContext();
            var auditService = new AuditService(ctx);
            var emailService = new Mock<IEmailService>();
            var configuration = new Mock<IConfiguration>();
            return new PeriodLockController(
                new PeriodLockRepository(ctx),
                new UnlockRequestRepository(ctx),
                new PeriodLockService(),
                auditService,
                emailService.Object,
                configuration.Object
            ).WithHttpContext(AdminUser);
        }

        private PeriodLockController NewUserController()
        {
            var ctx = _fixture.CreateContext();
            var auditService = new AuditService(ctx);
            var emailService = new Mock<IEmailService>();
            var configuration = new Mock<IConfiguration>();
            return new PeriodLockController(
                new PeriodLockRepository(ctx),
                new UnlockRequestRepository(ctx),
                new PeriodLockService(),
                auditService,
                emailService.Object,
                configuration.Object
            ).WithHttpContext(RegularUser);
        }

        // ─────────────────────────────────────────────────────────────
        // EC-1 — Dvostruko zaključavanje
        // Očekivano: drugi lock vraća 400 "Period je već zaključan."
        // ─────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task Lock_WhenAlreadyLocked_Returns400WithMessage()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange — prvo zaključavanje treba proći
            var firstResult = await NewAdminController().LockPeriod();
            firstResult.Should().BeOfType<OkObjectResult>();

            // Act — pokušaj zaključati opet (novi kontroler = novi DB kontekst, kao novi HTTP zahtjev)
            var secondResult = await NewAdminController().LockPeriod();

            // Assert
            var bad = secondResult.Should().BeOfType<BadRequestObjectResult>().Subject;
            var json = System.Text.Json.JsonSerializer.Serialize(bad.Value);
            json.Should().Contain("Period je već zaključan");
        }

        // ─────────────────────────────────────────────────────────────
        // EC-2 — Dvostruko otključavanje
        // Očekivano: drugi unlock vraća 400 "Period nije zaključan."
        // ─────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task Unlock_WhenAlreadyUnlocked_Returns400WithMessage()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange — zaključaj pa otključaj
            await NewAdminController().LockPeriod();
            var firstUnlock = await NewAdminController().UnlockPeriod();
            firstUnlock.Should().BeOfType<OkObjectResult>();

            // Act — pokušaj otključati opet
            var secondUnlock = await NewAdminController().UnlockPeriod();

            // Assert
            var bad = secondUnlock.Should().BeOfType<BadRequestObjectResult>().Subject;
            var json = System.Text.Json.JsonSerializer.Serialize(bad.Value);
            json.Should().Contain("Period nije zaključan");
        }

        // ─────────────────────────────────────────────────────────────
        // EC-3 — Validacija razloga zahtjeva
        // ─────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task RequestUnlock_WithEmptyReason_Returns400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var result = await NewUserController().RequestUnlock(new RequestUnlockDto { Reason = "" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [SkippableFact]
        public async Task RequestUnlock_WithReasonUnder10Chars_Returns400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // "Kratko" = 6 karaktera, ispod minimuma od 10
            var result = await NewUserController().RequestUnlock(new RequestUnlockDto { Reason = "Kratko" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [SkippableFact]
        public async Task RequestUnlock_WithReasonOver500Chars_Returns400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // 501 karakter, iznad maksimuma od 500
            var longReason = new string('a', 501);
            var result = await NewUserController().RequestUnlock(new RequestUnlockDto { Reason = longReason });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [SkippableFact]
        public async Task RequestUnlock_WithValidReason_Returns200()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Validan razlog — između 10 i 500 karaktera
            var result = await NewUserController().RequestUnlock(new RequestUnlockDto
            {
                Reason = "Potrebna korekcija podataka za klijenta zbog greške pri unosu."
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─────────────────────────────────────────────────────────────
        // EC-4 — Više zahtjeva istog korisnika
        // Oba zahtjeva moraju biti primljena i vidljiva u admin tabeli
        // ─────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task RequestUnlock_SameUserSendsMultipleRequests_BothAreAccepted()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var dto = new RequestUnlockDto
            {
                Reason = "Potrebna korekcija podataka za klijenta zbog greške pri unosu."
            };

            // Isti korisnik šalje dva zahtjeva (svaki put novi kontroler = novi kontekst)
            var first = await NewUserController().RequestUnlock(dto);
            var second = await NewUserController().RequestUnlock(dto);

            first.Should().BeOfType<OkObjectResult>();
            second.Should().BeOfType<OkObjectResult>();
        }

        [SkippableFact]
        public async Task RequestUnlock_AfterTwoRequests_AdminSeesAtLeastTwoPendingEntries()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var dto = new RequestUnlockDto
            {
                Reason = "Potrebna korekcija podataka za klijenta zbog greške pri unosu."
            };

            // Pošalji dva zahtjeva
            await NewUserController().RequestUnlock(dto);
            await NewUserController().RequestUnlock(dto);

            // Admin pregleda zahtjeve
            var result = await NewAdminController().GetUnlockRequests("PENDING", 1, 20);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);

            // Provjeri da postoje barem 2 PENDING zahtjeva
            json.Should().Contain("\"total\":2",
                "Admin tabela treba prikazivati oba PENDING zahtjeva.");
        }

        // ─────────────────────────────────────────────────────────────
        // EC-5 — Neautorizovan pristup endpointima
        //
        // Autorizacijski atributi su middleware pravila
        // koji se ne izvršavaju pri direktnom instanciranju kontrolera. Autorizacija
        // se provjerava u e2e testovima s Keycloak-om. Ovdje dokumentujemo očekivanje
        // kroz provjeru HTTP atributa na samom kontroleru.
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void PeriodController_RequiresRegulatoryReportingAccess()
        {
            var authorizeAttr = typeof(PeriodLockController)
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .FirstOrDefault();

            authorizeAttr.Should().NotBeNull("upravljanje periodom zahtijeva autentificiran funkcionalni pristup");
            authorizeAttr!.Roles.Should().Be("regulatory-reporting");
        }

        [Fact]
        public void GetCurrentEndpoint_InheritsControllerAuthorization()
        {
            var method = typeof(PeriodLockController).GetMethod(nameof(PeriodLockController.GetCurrentState));
            method.Should().NotBeNull();

            var allowAnonAttr = method!
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true)
                .FirstOrDefault();

            allowAnonAttr.Should().BeNull("podaci o periodu nisu javni i koriste isto pravilo pristupa kao modul izvještavanja");
        }

        // ─────────────────────────────────────────────────────────────
        // EC-6 — Audit log provjera
        //
        // Audit log provjera
        // ─────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task Lock_CreatesAuditLogEntry_WithPeriodLockAction()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange — zaključaj period
            await NewAdminController().LockPeriod();

            // Assert — provjeri direktno u bazi
            await using var ctx = _fixture.CreateContext();
            var log = ctx.AuditLogs
                .Where(l => l.Action == "PERIOD_LOCK")
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            log.Should().NotBeNull("Lock mora kreirati AuditLog s Action=PERIOD_LOCK");
            log!.Username.Should().NotBeNullOrEmpty("Username mora biti popunjen");
            log.UserId.Should().NotBeNullOrEmpty("UserId mora biti popunjen");
            log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [SkippableFact]
        public async Task Unlock_CreatesAuditLogEntry_WithPeriodUnlockAction()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange — zaključaj pa otključaj
            await NewAdminController().LockPeriod();
            await NewAdminController().UnlockPeriod();

            // Assert — provjeri direktno u bazi
            await using var ctx = _fixture.CreateContext();
            var log = ctx.AuditLogs
                .Where(l => l.Action == "PERIOD_UNLOCK")
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            log.Should().NotBeNull("Unlock mora kreirati AuditLog s Action=PERIOD_UNLOCK");
            log!.Username.Should().NotBeNullOrEmpty("Username mora biti popunjen");
            log.UserId.Should().NotBeNullOrEmpty("UserId mora biti popunjen");
            log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }
    }
}
