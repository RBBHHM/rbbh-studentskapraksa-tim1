using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Security;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// Application-owned documentation, not a reusable platform business schema.
public sealed record GuideSection(string Id, string? Permission, int Points, bool Administrator = false);
public static class GuideContent
{
    private static readonly GuideSection[] Sections = [new("physical", "FL_VIEW", 6), new("legal", "PL_VIEW", 3),
        new("limits", "LIMITI_VIEW", 4), new("capital", "KAPITAL_VIEW", 2), new("reports", null, 3),
        new("notifications", "KAPITAL_VIEW", 2), new("administration", "ADMINISTRATION_MANAGE", 5, true)];
    public static IReadOnlyList<GuideSection> Visible(ApplicationProfile profile) => Sections.Where(section =>
        (section.Permission is null ? ApplicationAccess.HasEffectiveApplicationAccess(profile) : profile.Permissions.Contains(section.Permission)) &&
        (!section.Administrator || ApplicationAccess.IsAdministrator(profile))).ToArray();
}
