namespace RBBH.ConnectedParties.Helpers.Constants;

public static class ApplicationPermissions
{
    public const string PhysicalPersonsView = "FL_VIEW";
    public const string PhysicalPersonsCreate = "FL_CREATE";
    public const string PhysicalPersonsEdit = "FL_EDIT";
    public const string PhysicalPersonsDelete = "FL_DELETE";
    public const string LegalPersonsView = "PL_VIEW";
    public const string LegalPersonsCreate = "PL_CREATE";
    public const string LegalPersonsEdit = "PL_EDIT";
    public const string LegalPersonsDelete = "PL_DELETE";
    public const string CapitalView = "KAPITAL_VIEW";
    public const string CapitalCreate = "KAPITAL_CREATE";
    public const string CapitalEdit = "KAPITAL_EDIT";
    public const string CapitalDelete = "KAPITAL_DELETE";
    public const string LimitsView = "LIMITI_VIEW";
    public const string LimitsCreate = "LIMITI_CREATE";
    public const string LimitsEdit = "LIMITI_EDIT";
    public const string LimitsDelete = "LIMITI_DELETE";
    public const string RegulatoryReportingView = "REGULATORNA_IZVJESTAVANJA_VIEW";
    public const string RegulatoryReportingExport = "REGULATORNA_IZVJESTAVANJA_EXPORT";
    public const string CodeListsView = "CODE_LISTS_VIEW";
    public const string AdministrationView = "ADMINISTRATION_VIEW";
    public const string AdministrationManage = "ADMINISTRATION_MANAGE";

    public static readonly string[] All =
    [
        PhysicalPersonsView, PhysicalPersonsCreate, PhysicalPersonsEdit, PhysicalPersonsDelete,
        LegalPersonsView, LegalPersonsCreate, LegalPersonsEdit, LegalPersonsDelete,
        CapitalView, CapitalCreate, CapitalEdit, CapitalDelete,
        LimitsView, LimitsCreate, LimitsEdit, LimitsDelete,
        RegulatoryReportingView, RegulatoryReportingExport,
        CodeListsView,
        AdministrationView, AdministrationManage
    ];
}