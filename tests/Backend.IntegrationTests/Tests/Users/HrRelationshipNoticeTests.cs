using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

public sealed class HrRelationshipNoticeTests : AuthenticatedApiFixture
{
    [Fact]
    public async Task NewAndExpiredRelationshipsQueueOnlyOneMailEachForActiveHr()
    {
        var today = DateTime.UtcNow.Date;
        await InDatabase(async db =>
        {
            var role = await db.Roles.SingleAsync(row => row.Name == ApplicationAccessRoles.HumanResources);
            foreach (var active in new[] { true, false })
            {
                var name = active ? "active-hr" : "inactive-hr";
                var user = new AppUser { Username = name, Email = name + "@example.test", KeycloakId = name,
                    KeycloakSubjectConfirmed = true, IsActive = active };
                db.AppUsers.Add(user);
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }
            await db.SaveChangesAsync();

            var person = new RelatedPerson { FirstName = "Fixture", LastName = "Individual",
                RelationBasis = "VL", DateTo = today, CreatedBy = "fixture" };
            db.RelatedPersons.Add(person);
            var notices = new HrRelationshipNotices(db);
            await notices.StageNewAsync(default);
            await db.SaveChangesAsync();

            await notices.StageExpiredAsync(today, default);
            Assert.Single(await db.Notifications.ToListAsync());
            await notices.StageExpiredAsync(today.AddDays(1), default);
            await notices.StageExpiredAsync(today.AddDays(1), default);
            var rows = await db.Notifications.Include(row => row.Delivery).ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => { Assert.Equal("active-hr", row.RecipientSubject); Assert.NotNull(row.Delivery); });
            Assert.Contains(rows, row => row.TitleKey.EndsWith(".added.title"));
            Assert.Contains(rows, row => row.TitleKey.EndsWith(".ended.title"));
        });
    }
}
