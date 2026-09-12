namespace AgriCheck.Application.Auth;

public static class RoleHierarchy
{
    private static readonly Dictionary<string, string[]> Hierarchy = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ROLE_ADMIN"] = new[] { "ROLE_EVALUATOR", "ROLE_BILLING_AGENT", "ROLE_ACCREDITATION_OFFICER", "ROLE_MAV_ADMIN", "ROLE_MAV_SECRETARY", "ROLE_WAREHOUSE_STAFF", "ROLE_DRIVER", "ROLE_USER" },
        ["ROLE_MAV_ADMIN"] = new[] { "ROLE_MAV_SECRETARY", "ROLE_USER" },
        ["ROLE_MAV_EVALUATOR"] = new[] { "ROLE_USER" },
        ["ROLE_MAV_SECRETARY"] = new[] { "ROLE_USER" },
        ["ROLE_EVALUATOR"] = new[] { "ROLE_USER" },
        ["ROLE_ACCOUNTANT"] = new[] { "ROLE_USER" },
        ["ROLE_SECRETARY"] = new[] { "ROLE_USER" },
        ["ROLE_UNDERSECRETARY"] = new[] { "ROLE_USER" },
        ["ROLE_BILLING_AGENT"] = new[] { "ROLE_USER" },
        ["ROLE_ACCREDITATION_OFFICER"] = new[] { "ROLE_USER" },
        ["ROLE_MAV_IMPORTER"] = new[] { "ROLE_USER" },
        ["ROLE_AGENCY_ADMIN"] = new[] { "ROLE_USER" },
        ["ROLE_AGENCY"] = new[] { "ROLE_USER" },
        ["ROLE_AGENCY_USER"] = new[] { "ROLE_USER" },
        ["ROLE_INSPECTOR"] = new[] { "ROLE_USER" },
        ["ROLE_DOCTOR"] = new[] { "ROLE_INSPECTOR", "ROLE_USER" },
        ["ROLE_OPERATOR"] = new[] { "ROLE_USER" },
        ["ROLE_DRIVER"] = new[] { "ROLE_USER" },
        ["ROLE_WAREHOUSE_STAFF"] = new[] { "ROLE_USER" },
        ["ROLE_DA_SECRETARY"] = new[] { "ROLE_USER" },
        ["ROLE_DA_UNDERSECRETARY"] = new[] { "ROLE_USER" },
        ["ROLE_IMPORTER"] = new[] { "ROLE_USER" },
        ["ROLE_EXPORTER"] = new[] { "ROLE_USER" },
        ["ROLE_BROKER"] = new[] { "ROLE_USER" },
        ["ROLE_USER"] = Array.Empty<string>()
    };

    public static IReadOnlyList<string> ExpandRoles(IEnumerable<string> assignedRoles)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(assignedRoles);

        while (queue.Count > 0)
        {
            var role = queue.Dequeue();
            if (!result.Add(role))
            {
                continue;
            }

            if (Hierarchy.TryGetValue(role, out var implied))
            {
                foreach (var child in implied)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result.OrderBy(r => r).ToList();
    }

    public static string ResolveRedirectPath(IEnumerable<string> roles)
    {
        var set = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);

        if (set.Contains("ROLE_ADMIN")) return "/admin";
        if (set.Contains("ROLE_MAV_ADMIN") || set.Contains("ROLE_MAV_SECRETARY") || set.Contains("ROLE_MAV_EVALUATOR") || set.Contains("ROLE_MAV_IMPORTER")) return "/mav";
        if (set.Contains("ROLE_INSPECTOR") || set.Contains("ROLE_DOCTOR")) return "/inspector";
        if (set.Contains("ROLE_DA_SECRETARY") || set.Contains("ROLE_DA_UNDERSECRETARY")) return "/da";
        if (set.Contains("ROLE_ACCREDITATION_OFFICER") &&
            !set.Contains("ROLE_EVALUATOR") &&
            !set.Contains("ROLE_ACCOUNTANT") &&
            !set.Contains("ROLE_SECRETARY") &&
            !set.Contains("ROLE_UNDERSECRETARY") &&
            !set.Contains("ROLE_BILLING_AGENT") &&
            !set.Contains("ROLE_AGENCY_ADMIN") &&
            !set.Contains("ROLE_AGENCY") &&
            !set.Contains("ROLE_AGENCY_USER"))
        {
            return "/accreditation-officer/dashboard";
        }
        if (set.Contains("ROLE_EVALUATOR") || set.Contains("ROLE_ACCOUNTANT") || set.Contains("ROLE_SECRETARY")
            || set.Contains("ROLE_UNDERSECRETARY") || set.Contains("ROLE_BILLING_AGENT") || set.Contains("ROLE_ACCREDITATION_OFFICER")
            || set.Contains("ROLE_AGENCY_ADMIN") || set.Contains("ROLE_AGENCY") || set.Contains("ROLE_AGENCY_USER")) return "/agency";
        if (set.Contains("ROLE_WAREHOUSE_STAFF")) return "/warehouse";
        if (set.Contains("ROLE_DRIVER") || set.Contains("ROLE_OPERATOR")) return "/driver";
        return "/client";
    }

    public static IReadOnlyList<string> AllRoleCodes => new List<string>
    {
        "ROLE_ADMIN", "ROLE_MAV_ADMIN", "ROLE_MAV_EVALUATOR", "ROLE_MAV_SECRETARY", "ROLE_MAV_IMPORTER",
        "ROLE_AGENCY_ADMIN", "ROLE_AGENCY", "ROLE_AGENCY_USER",
        "ROLE_EVALUATOR", "ROLE_ACCOUNTANT", "ROLE_SECRETARY", "ROLE_UNDERSECRETARY",
        "ROLE_BILLING_AGENT", "ROLE_ACCREDITATION_OFFICER",
        "ROLE_INSPECTOR", "ROLE_DOCTOR", "ROLE_OPERATOR", "ROLE_DRIVER",
        "ROLE_DA_SECRETARY", "ROLE_DA_UNDERSECRETARY",
        "ROLE_WAREHOUSE_STAFF", "ROLE_IMPORTER", "ROLE_EXPORTER", "ROLE_BROKER", "ROLE_USER"
    };
}
