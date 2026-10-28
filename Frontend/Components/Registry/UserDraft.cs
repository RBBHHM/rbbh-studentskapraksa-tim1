using System.ComponentModel.DataAnnotations;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public sealed class UserRoleChoice
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public bool Selected { get; set; }
}
public sealed class UserDraft
{
    public Guid? Id { get; init; }
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public List<UserRoleChoice> Roles { get; init; } = [];
    public Guid[] SelectedRoles => Roles.Where(role => role.Selected).Select(role => role.Id).ToArray();
    public static UserDraft From(ApplicationUser? user, RolesCatalog catalog) => new()
    {
        Id = user?.Id,
        Username = user?.Username ?? "",
        FirstName = user?.FirstName ?? "",
        LastName = user?.LastName ?? "",
        Email = user?.Email ?? "",
        Roles = catalog.Roles.Where(role => !role.Name.Equals("gost", StringComparison.OrdinalIgnoreCase))
            .Select(role => new UserRoleChoice { Id = role.Id, Name = role.Name, Selected = user?.Roles.Contains(role.Name) == true }).ToList()
    };
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (Id is not null) return errors; // Empty edit deliberately preserves removal/Guest semantics, subject to backend safety.
        foreach (var (field, value) in new[] { (nameof(Username), Username), (nameof(FirstName), FirstName), (nameof(LastName), LastName) })
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100) errors[field] = ["users.name-invalid"];
        if (!new EmailAddressAttribute().IsValid(Email.Trim()) || !Email.Trim().EndsWith("@raiffeisengroup.ba", StringComparison.OrdinalIgnoreCase))
            errors[nameof(Email)] = ["users.email-invalid"];
        return errors;
    }
    public CreateApplicationUser ToRequest()
    {
        if (Id is not null || Validate().Count > 0) throw new InvalidOperationException("Resolve create-user validation before sending.");
        return new(Username.Trim(), FirstName.Trim(), LastName.Trim(), Email.Trim().ToLowerInvariant(), SelectedRoles);
    }
}
