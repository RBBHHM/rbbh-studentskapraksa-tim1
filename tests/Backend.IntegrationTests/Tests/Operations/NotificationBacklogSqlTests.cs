using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Notifications;
using RBBH.ConnectedParties.Services;
using IntegrationTests.Tests.Users;

namespace IntegrationTests.Tests.Operations;

public sealed class NotificationBacklogSqlTests : RoleRequestFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task BacklogDistinguishesPendingLeasedExhaustedAndDeliveredWithoutOpeningPayloads()
    {
        await InDatabase(async db =>
        {
            Assert.Equal(new NotificationBacklog(0, 0, 0), await BackgroundOperationsMetrics.ObserveBacklog(db, default));
            foreach (var state in new[] { "pending", "leased", "exhausted", "delivered" })
            {
                var notice = new ApplicationNotification { RecipientSubject = "private-subject", RecipientEmail = "private@example.test",
                    EventKey = state, ParametersJson = "not JSON; monitoring must never deserialize it", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
                notice.Delivery = new NotificationDelivery { NotificationId = notice.Id, Attempts = state == "exhausted" ? 5 : 1,
                    DeliveredAt = state == "delivered" ? DateTime.UtcNow : null,
                    LeaseId = state == "leased" ? Guid.NewGuid() : null, LeaseUntil = state == "leased" ? DateTime.UtcNow.AddMinutes(1) : null };
                db.Notifications.Add(notice);
            }
            await db.SaveChangesAsync();
            var snapshot = await BackgroundOperationsMetrics.ObserveBacklog(db, default);
            Assert.Equal(2, snapshot.Pending); Assert.Equal(1, snapshot.Exhausted);
            Assert.InRange(snapshot.OldestPendingSeconds, 600, 660);
            Assert.Equal(4, await db.NotificationDeliveries.CountAsync());
            Assert.Equal(1, await db.NotificationDeliveries.CountAsync(row => row.DeliveredAt != null));
        });
    }
}
