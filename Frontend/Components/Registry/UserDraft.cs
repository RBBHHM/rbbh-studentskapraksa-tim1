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
    public const string EmailDomain = "@raiffeisengroup.ba";
    private string _suggestedEmail = "";
    public string Email { get; set; } = "";
    /// <summary>Pre-fills the corporate address while the user has not typed their own; it stays editable.</summary>
    public void UsernameChanged()
    {
        if (Id is not null || (Email.Length > 0 && Email != _suggestedEmail)) return;
        _suggestedEmail = Username.Trim().Length == 0 ? "" : Username.Trim() + EmailDomain;
        Email = _suggestedEmail;
    }
    private (string Username, string FirstName, string LastName, string Email) _original;
    /// <summary>True when an existing user's profile fields differ from what the server returned.</summary>
    public bool ProfileChanged => Id is not null && (Username.Trim(), FirstName.Trim(), LastName.Trim(), Email.Trim()) != _original;
    public bool RolesChanged(ApplicationUser current) => !current.Roles.Order(StringComparer.OrdinalIgnoreCase).SequenceEqual(Roles.Where(role => role.Selected).Select(role => role.Name).Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    public List<UserRoleChoice> Roles { get; init; } = [];
    public Guid[] SelectedRoles => Roles.Where(role => role.Selected).Select(role => role.Id).ToArray();
    public static UserDraft From(ApplicationUser? user, RolesCatalog catalog) => new()
    {
        _original = (user?.Username.Trim() ?? "", user?.FirstName.Trim() ?? "", user?.LastName.Trim() ?? "", user?.Email.Trim() ?? ""),
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
        foreach (var (field, value) in new[] { (nameof(Username), Username), (nameof(FirstName), FirstName), (nameof(LastName), LastName) })
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100) errors[field] = ["users.name-invalid"];
        // The corporate domain is only a suggestion; any valid address is accepted (people outside the domain exist).
        if (Email.Trim().Length > 200 || !new EmailAddressAttribute().IsValid(Email.Trim()))
            errors[nameof(Email)] = ["users.email-invalid"];
        return errors;
    }
    public CreateApplicationUser ToRequest()
    {
        if (Id is not null || Validate().Count > 0) throw new InvalidOperationException("Resolve create-user validation before sending.");
        return new(Username.Trim(), FirstName.Trim(), LastName.Trim(), Email.Trim().ToLowerInvariant(), SelectedRoles);
    }
}
