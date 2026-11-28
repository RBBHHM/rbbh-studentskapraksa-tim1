using System.Net.Mail;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Notifications;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class HrRelationshipNotices(ConnectedPartiesDbContext db)
{
    internal const string KeyPrefix = "notifications.hr.relationship.";

    internal static IQueryable<AppUser> Recipients(ConnectedPartiesDbContext context) =>
        context.AppUsers.Where(user => user.IsActive && user.KeycloakSubjectConfirmed && user.KeycloakId != "" &&
            context.UserRoles.Any(grant => grant.UserId == user.Id && grant.IsActive && grant.Role.IsActive &&
                grant.Role.Name == ApplicationAccessRoles.HumanResources));

    public async Task StageNewAsync(CancellationToken ct)
    {
        var added = db.ChangeTracker.Entries<RelatedPerson>().Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity).ToArray();
        if (added.Length == 0) return;
        var recipients = await LoadRecipients(ct);
        var labels = recipients.Length == 0 ? new() : await LoadBasisLabels(ct);
        foreach (var person in added)
        {
            Stage(person, recipients, labels, "added");
            // An already-ended relationship is described in the creation notice.
            if (person.DateTo < DateTime.UtcNow.Date) person.HrExpiryNotifiedFor = person.DateTo;
        }
    }

    public async Task StageExpiredAsync(DateTime today, CancellationToken ct)
    {
        var people = await db.RelatedPersons.AsTracking().Where(person => person.IsActive &&
                person.DateTo < today && person.HrExpiryNotifiedFor != person.DateTo)
            .OrderBy(person => person.DateTo).ThenBy(person => person.Id).Take(100).ToListAsync(ct);
        if (people.Count == 0) return;
        var recipients = await LoadRecipients(ct);
        var labels = recipients.Length == 0 ? new() : await LoadBasisLabels(ct);
        foreach (var person in people)
        {
            Stage(person, recipients, labels, "ended");
            person.HrExpiryNotifiedFor = person.DateTo;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Recipient[]> LoadRecipients(CancellationToken ct)
    {
        var rows = await Recipients(db).Select(user => new Recipient(user.KeycloakId, user.Email)).ToArrayAsync(ct);
        return rows.Where(row => MailAddress.TryCreate(row.Email, out var address) && address.Address == row.Email).ToArray();
    }

    private async Task<Dictionary<string, string>> LoadBasisLabels(CancellationToken ct)
    {
        var labels = await db.CodeLists.IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.Definition!.Name == "OSNOV_POVEZANOSTI")
            .Select(row => new { row.Kod, row.Naziv }).ToListAsync(ct);
        return labels.GroupBy(row => row.Kod, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Naziv, StringComparer.OrdinalIgnoreCase);
    }

    private void Stage(RelatedPerson person, Recipient[] recipients, Dictionary<string, string> labels, string kind)
    {
        if (recipients.Length == 0) return;
        var payload = JsonSerializer.Serialize(new
        {
            fullName = person.FirstName + " " + person.LastName,
            basis = labels.GetValueOrDefault(person.RelationBasis ?? "") ?? person.RelationBasis,
            from = person.DateFrom?.ToString("dd.MM.yyyy"),
            until = person.DateTo?.ToString("dd.MM.yyyy")
        });
        var eventKey = $"hr-relationship:{kind}:{Guid.NewGuid():N}";
        foreach (var recipient in recipients)
        {
            var notice = new ApplicationNotification
            {
                RecipientSubject = recipient.Subject, RecipientEmail = recipient.Email, EventKey = eventKey,
                TitleKey = KeyPrefix + kind + ".title", BodyKey = KeyPrefix + kind + ".body",
                ParametersJson = payload, Route = "/app/physical-persons"
            };
            notice.Delivery = new NotificationDelivery { Notification = notice };
            db.Notifications.Add(notice);
        }
    }

    private sealed record Recipient(string Subject, string Email);
}
