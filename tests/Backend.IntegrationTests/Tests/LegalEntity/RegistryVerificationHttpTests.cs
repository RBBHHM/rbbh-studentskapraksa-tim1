using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;

namespace IntegrationTests.Tests.LegalEntity;

public class RegistryVerificationHttpTests : AuthenticatedApiFixture
{
    internal static object Legal(string status = "Draft") => new
    {
        name = "Fixture company", isResident = false, fbaId = "12345", basisOfConnection = "VL",
        connectionDescription = "Fixture relationship", connectedWithBank = true, dateFrom = "2026-01-01", status,
        creatorSubject = "forged-creator"
    };

    internal static object Physical() => new
    {
        firstName = "Fixture", lastName = "Person", residency = 1, jmbg = "2801984175000",
        relationBasis = "VL", relationDescription = "Fixture relationship", specialRelationBasis = "GOV",
        isIdentifiedStaff = true, dateFrom = "2026-01-01", dateTo = "2027-12-31", creatorSubject = "forged-creator"
    };

    protected async Task<Guid> CreateRecord(bool legal)
    {
        var response = await Client.PostAsJsonAsync(legal ? "/api/legal-entities" : "/api/related-persons", legal ? Legal() : Physical());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    protected static string VerifyRoute(bool legal, Guid id) => $"/api/verification/{(legal ? "legal" : "physical")}-person/{id}";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreatorCannotVerifyEvenAfterEmailChangeAndForgedCreatorBody(bool legal)
    {
        var id = await CreateRecord(legal);
        Client.DefaultRequestHeaders.Add("X-Email", "changed@example.test");
        var response = await Client.PostAsJsonAsync(VerifyRoute(legal, id), new { creatorSubject = "other", verifiedBy = "other" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("verification.self_denied", await response.Content.ReadAsStringAsync());
        await InDatabase(async db =>
        {
            var creator = legal ? (await db.LegalEntities.FindAsync(id))!.CreatorSubject : (await db.RelatedPersons.FindAsync(id))!.CreatorSubject;
            Assert.Equal("creator", creator);
            Assert.Empty(await db.AuditLogs.Where(row => row.Action == "VERIFY").ToListAsync());
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OtherVerifierSucceedsOnceWithOneAuditAndPeriodLockStillApplies(bool legal)
    {
        var id = await CreateRecord(legal);
        using var reviewer = NewClient("reviewer");
        await InDatabase(async db => { var period = await db.PeriodLocks.SingleAsync(); period.IsLocked = true; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Locked, (await reviewer.PostAsync(VerifyRoute(legal, id), null)).StatusCode);
        await InDatabase(async db => { var period = await db.PeriodLocks.SingleAsync(); period.IsLocked = false; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.OK, (await reviewer.PostAsync(VerifyRoute(legal, id), null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await reviewer.PostAsync(VerifyRoute(legal, id), null)).StatusCode);
        await InDatabase(async db =>
        {
            var audit = Assert.Single(await db.AuditLogs.Where(row => row.Action == "VERIFY").ToListAsync());
            Assert.Equal("reviewer", audit.UserId);
            Assert.Equal("reviewer", audit.Username);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HistoricalRecordRequiresProvenanceInsteadOfGuessingFromEmail(bool legal)
    {
        Guid id = Guid.Empty;
        await InDatabase(async db =>
        {
            if (legal) { var row = new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity { Name = "History", CreatedBy = "someone@example.test" }; db.LegalEntities.Add(row); id = row.Id; }
            else { var row = new RelatedPerson { FirstName = "History", CreatedBy = "someone@example.test" }; db.RelatedPersons.Add(row); id = row.Id; }
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsync(VerifyRoute(legal, id), null)).StatusCode);
    }

    [Fact]
    public async Task StatusOverpostingCannotVerifyThroughOrdinaryLegalUpdate()
    {
        var id = await CreateRecord(true);
        var response = await Client.PutAsJsonAsync($"/api/legal-entities/{id}", Legal("Verified"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await InDatabase(async db => Assert.Equal("Draft", (await db.LegalEntities.FindAsync(id))!.Status));
    }

    [Fact]
    public async Task CreatorSubjectIsImmutableAtPersistenceBoundary()
    {
        var id = await CreateRecord(true);
        await InDatabase(async db =>
        {
            var row = (await db.LegalEntities.FindAsync(id))!;
            db.Entry(row).Property("CreatorSubject").CurrentValue = "forged";
            await Assert.ThrowsAsync<RBBH.ConnectedParties.Exceptions.BusinessRuleException>(() => db.SaveChangesAsync());
        });
        await InDatabase(async db => Assert.Equal("creator", (await db.LegalEntities.FindAsync(id))!.CreatorSubject));
    }

    [Fact]
    public async Task PhysicalUpdateCannotOverpostVerificationOrCreator()
    {
        var id = await CreateRecord(false);
        var body = JsonSerializer.SerializeToElement(Physical());
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body)!;
        values["status"] = JsonSerializer.SerializeToElement("Verified");
        Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync($"/api/related-persons/{id}", values)).StatusCode);
        await InDatabase(async db =>
        {
            var row = (await db.RelatedPersons.FindAsync(id))!;
            Assert.Equal(RelatedPersonStatus.Draft, row.Status); Assert.Equal("creator", row.CreatorSubject);
        });
    }
}
