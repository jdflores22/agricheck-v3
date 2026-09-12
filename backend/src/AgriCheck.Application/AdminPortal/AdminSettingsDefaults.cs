namespace AgriCheck.Application.AdminPortal;

public static class AdminSettingsDefaults
{
    public static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["system_name"] = "AgriCheck System",
        ["system_description"] = "",
        ["contact_email"] = "",
        ["support_phone"] = "",
        ["timezone"] = "Asia/Manila",
        ["date_format"] = "Y-m-d",
        ["primary_color"] = "#166534",
        ["secondary_color"] = "#4caf50",
        ["footer_text"] = "© 2025 Department of Agriculture. All rights reserved.",
        ["spinner_color"] = "#166534",
        ["system_logo_path"] = "",
        ["spinner_logo_path"] = "",
        ["favicon_path"] = "",
        ["smtp_host"] = "",
        ["smtp_port"] = "587",
        ["smtp_encryption"] = "tls",
        ["smtp_username"] = "",
        ["from_email"] = "",
        ["from_name"] = "AgriCheck System",
        ["max_file_size"] = "10",
        ["allowed_file_types"] = "pdf,jpg,jpeg,png,doc,docx,xls,xlsx",
        ["file_retention_days"] = "365",
        ["session_timeout"] = "60",
        ["max_login_attempts"] = "5",
        ["min_password_length"] = "8",
        ["require_uppercase"] = "1",
        ["require_numbers"] = "1",
        ["require_special_chars"] = "0",
        ["enable_email_notifications"] = "1",
        ["notify_on_submission"] = "1",
        ["notify_on_approval"] = "1",
        ["enable_inapp_notifications"] = "1",
        ["autosave_interval"] = "60",
        ["required_indicator"] = "*",
        ["accreditation_validity_days"] = "365",
        ["renewal_reminder_days"] = "60",
        ["compliance_deadline_days"] = "30",
        ["maintenance_mode"] = "0",
        ["maintenance_message"] = "System is currently under maintenance. Please check back later.",
        ["maintenance_allowed_ips"] = "",
    };

    public static readonly IReadOnlySet<string> AgencyRoleCodes = RoleDefinitions.AgencyRoleCodes;
}
