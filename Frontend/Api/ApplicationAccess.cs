namespace RBBH.RelatedPartiesRegister.Web.Api;

public static class ApplicationAccess
{
    // The API supplies virtual Guest for eligible identities and an empty role list for denied identities.
    public static bool HasEffectiveApplicationAccess(ApplicationProfile profile) => profile.Roles.Count > 0;

    public static bool CanViewAdministration(ApplicationProfile profile) =>
        profile.Permissions.Contains("ADMINISTRATION_VIEW", StringComparer.Ordinal);

    public static bool CanManageAdministration(ApplicationProfile profile) =>
        CanViewAdministration(profile) && profile.Permissions.Contains("ADMINISTRATION_MANAGE", StringComparer.Ordinal);
}
