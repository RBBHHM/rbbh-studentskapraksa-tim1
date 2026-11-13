using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;

namespace IntegrationTests.Tests.LegalEntity;

/// <summary>
/// Relational regression: default SQL collation must not change the user-facing
/// case-insensitive search contract for both registry modules.
/// Uses only the test's disposable SQL Server database.
/// </summary>
public sealed class RegistrySearchSqlTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task CaseInsensitiveSearchMatchesBothRegistriesOnRealSqlServer()
    {
        await InDatabase(async db =>
        {
            db.LegalEntities.Add(new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity
            {
                Name = "MiXeD Legal Fixture",
                IsActive = true,
                CreatedBy = "fixture"
            });
            db.RelatedPersons.Add(new RBBH.ConnectedParties.DL.Entities.RelatedPersons.RelatedPerson
            {
                FirstName = "MiXeD",
                LastName = "PhysicalFixture",
                IsActive = true,
                CreatedBy = "fixture"
            });
            await db.SaveChangesAsync();
        });

        foreach (var route in new[]
        {
            "/api/legal-entities?search=mIxEd&page=1&pageSize=20",
            "/api/related-persons/page?search=mIxEd&page=1&pageSize=20"
        })
        {
            var response = await Client.GetFromJsonAsync<JsonElement>(route);
            Assert.Equal(1, response.GetProperty("total").GetInt32());
            Assert.Single(response.GetProperty("items").EnumerateArray());
            var upper = await Client.GetFromJsonAsync<JsonElement>(
                route.Replace("mIxEd", "MIXED", StringComparison.Ordinal));
            Assert.Equal(response.GetProperty("total").GetInt32(), upper.GetProperty("total").GetInt32());
        }
    }
}
