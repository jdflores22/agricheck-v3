namespace AgriCheck.Application.AdminPortal;

public static class RoleDefinitions
{
    public static readonly IReadOnlyList<(string Code, string Name, string Description)> All = new List<(string, string, string)>
    {
        ("ROLE_ADMIN", "System Administrator", "Full system administration access"),
        ("ROLE_MAV_ADMIN", "MAV Administrator", "Full MAV management access"),
        ("ROLE_MAV_EVALUATOR", "MAV Evaluator", "Review and evaluate MAV applications"),
        ("ROLE_MAV_SECRETARY", "MAV Secretary", "Read-only MAV reporting and records"),
        ("ROLE_MAV_IMPORTER", "MAV Importer", "Submit and manage own MAV applications"),
        ("ROLE_AGENCY_ADMIN", "Agency Administrator", "Agency-level administration"),
        ("ROLE_AGENCY", "Agency Staff", "General agency staff access"),
        ("ROLE_AGENCY_USER", "Agency Portal User", "Agency portal user access"),
        ("ROLE_EVALUATOR", "Agency Evaluator", "Evaluate and process agency entries"),
        ("ROLE_ACCOUNTANT", "Agency Accountant", "Manage agency billing and payments"),
        ("ROLE_SECRETARY", "Agency Secretary", "Agency records and user management"),
        ("ROLE_UNDERSECRETARY", "Agency Undersecretary", "Senior agency oversight access"),
        ("ROLE_DA_SECRETARY", "DA Secretary", "Department-wide oversight across all agencies"),
        ("ROLE_DA_UNDERSECRETARY", "DA Undersecretary", "Senior DA leadership oversight across all agencies"),
        ("ROLE_BILLING_AGENT", "Billing Agent", "Process billing and payment verification"),
        ("ROLE_ACCREDITATION_OFFICER", "DA Accreditation Officer", "Review and approve importer accreditation at the DA level (not agency-scoped)"),
        ("ROLE_INSPECTOR", "Inspector", "Conduct field inspections"),
        ("ROLE_DOCTOR", "Doctor", "Medical inspection and certification"),
        ("ROLE_OPERATOR", "Operator", "Operations portal access"),
        ("ROLE_DRIVER", "Driver", "Driver mobile and delivery access"),
        ("ROLE_WAREHOUSE_STAFF", "Warehouse Staff", "Warehouse inventory and release"),
        ("ROLE_IMPORTER", "Importer", "Import client portal access"),
        ("ROLE_EXPORTER", "Exporter", "Export client portal access"),
        ("ROLE_BROKER", "Broker", "Broker client portal access"),
        ("ROLE_USER", "User", "Base authenticated user access"),
    };

    public static readonly IReadOnlySet<string> AgencyRoleCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ROLE_AGENCY_ADMIN",
        "ROLE_AGENCY",
        "ROLE_AGENCY_USER",
        "ROLE_EVALUATOR",
        "ROLE_ACCOUNTANT",
        "ROLE_SECRETARY",
        "ROLE_UNDERSECRETARY",
        "ROLE_BILLING_AGENT",
        "ROLE_INSPECTOR",
        "ROLE_DOCTOR",
    };

    public static int SortKey(string code)
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i].Code, code, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return int.MaxValue;
    }
}
