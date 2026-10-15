namespace WfdHrms.Services.Identity;

public static class AppPermissions
{
    public const string CanCreateOnboarding = "can_create_onboarding";
    public const string CanApproveOnboarding = "can_approve_onboarding";
    public const string CanOverridePayroll = "can_override_payroll";
    public const string CanDeleteUnapprovedRequest = "can_delete_unapproved_request";
    public const string CanManageMasterData = "can_manage_master_data";
    public const string CanManageRbac = "can_manage_rbac";

    public static readonly Dictionary<string, string> Descriptions = new()
    {
        { CanCreateOnboarding, "Create and submit onboarding requests for mapped branches" },
        { CanApproveOnboarding, "Review, approve, request revision, or reject onboarding requests" },
        { CanOverridePayroll, "Override payroll firm and modify onboarding fields during review" },
        { CanDeleteUnapprovedRequest, "Delete or cancel requests that have not been approved" },
        { CanManageMasterData, "Manage Branches, Payroll Firms, Branch-Payroll Mappings, and User-Branch Assignments" },
        { CanManageRbac, "Manage Roles, Permission Matrices, and User Role Assignments" }
    };

    public static readonly string[] All =
    [
        CanCreateOnboarding,
        CanApproveOnboarding,
        CanOverridePayroll,
        CanDeleteUnapprovedRequest,
        CanManageMasterData,
        CanManageRbac
    ];
}

public static class CustomClaimTypes
{
    public const string Permission = "permission";
}
