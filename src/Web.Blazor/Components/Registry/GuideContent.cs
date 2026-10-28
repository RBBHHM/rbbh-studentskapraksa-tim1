using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Security;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// Application-owned documentation, not a reusable platform business schema.
public sealed record GuideSection(string Id, string[] Permissions, int Points, bool Administrator = false);
public static class GuideContent
{
    private static readonly GuideSection[] Sections = [new("physical", ["FL_VIEW"], 6), new("legal", ["PL_VIEW"], 3),
        new("limits", ["LIMITI_VIEW", "KAPITAL_VIEW"], 4), new("reports", [], 5), new("administration", ["ADMINISTRATION_MANAGE"], 4, true)];
    public static IReadOnlyList<GuideSection> Visible(ApplicationProfile profile) => Sections.Where(section =>
        (section.Permissions.Length == 0 ? ApplicationAccess.HasEffectiveApplicationAccess(profile) : section.Permissions.Any(profile.Permissions.Contains)) &&
        (!section.Administrator || ApplicationAccess.IsAdministrator(profile))).ToArray();
}
