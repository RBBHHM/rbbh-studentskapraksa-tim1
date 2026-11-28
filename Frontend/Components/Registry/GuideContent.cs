using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// Application-owned documentation, not a reusable platform business schema.
public sealed record GuideSection(string Id, string[] Permissions, int Points);
public static class GuideContent
{
    private static readonly GuideSection[] Sections = [new("physical", ["FL_VIEW"], 7), new("legal", ["PL_VIEW"], 5),
        new("limits", ["LIMITI_VIEW", "KAPITAL_VIEW"], 5), new("reports", [], 5), new("general", [], 4), new("administration", ["ADMINISTRATION_VIEW"], 5)];
    public static IReadOnlyList<GuideSection> Visible(ApplicationProfile profile) => Sections.Where(section =>
        section.Permissions.Length == 0 ? ApplicationAccess.HasEffectiveApplicationAccess(profile) : section.Permissions.Any(profile.Permissions.Contains)).ToArray();
}
