namespace RBBH.ConnectedParties.Helpers.Constants;

public static class RolePermissionMatrix
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [ApplicationAccessRoles.Guest] =
            [
                ApplicationPermissions.PhysicalPersonsView,
                ApplicationPermissions.LegalPersonsView,
                ApplicationPermissions.CapitalView,
                ApplicationPermissions.LimitsView,
                ApplicationPermissions.RegulatoryReportingView,
                ApplicationPermissions.RegulatoryReportingExport,
                ApplicationPermissions.CodeListsView
            ],
            [ApplicationAccessRoles.PhysicalPersons] =
            [
                ApplicationPermissions.PhysicalPersonsCreate,
                ApplicationPermissions.PhysicalPersonsEdit,
                ApplicationPermissions.PhysicalPersonsDelete
            ],
            [ApplicationAccessRoles.LegalPersons] =
            [
                ApplicationPermissions.LegalPersonsCreate,
                ApplicationPermissions.LegalPersonsEdit,
                ApplicationPermissions.LegalPersonsDelete
            ],
            [ApplicationAccessRoles.Capital] =
            [
                ApplicationPermissions.CapitalCreate,
                ApplicationPermissions.CapitalEdit,
                ApplicationPermissions.CapitalDelete
            ],
            [ApplicationAccessRoles.Limits] =
            [
                ApplicationPermissions.LimitsCreate,
                ApplicationPermissions.LimitsEdit,
                ApplicationPermissions.LimitsDelete
            ],
            [ApplicationAccessRoles.Administrator] = ApplicationPermissions.All
        };

    public static IReadOnlyCollection<string> Resolve(IEnumerable<string> explicitRoles) =>
        explicitRoles
            .Prepend(ApplicationAccessRoles.Guest)
            .Where(PermissionsByRole.ContainsKey)
            .SelectMany(role => PermissionsByRole[role])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}