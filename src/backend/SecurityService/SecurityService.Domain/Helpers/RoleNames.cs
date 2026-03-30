namespace SecurityService.Domain.Helpers;

public static class RoleNames
{
    public const string OrgAdmin = "OrgAdmin";
    public const int OrgAdminPermissionLevel = 100;

    public const string DeptLead = "DeptLead";
    public const int DeptLeadPermissionLevel = 75;

    public const string Member = "Member";
    public const int MemberPermissionLevel = 50;

    public const string Viewer = "Viewer";
    public const int ViewerPermissionLevel = 25;

    public const string PlatformAdmin = "PlatformAdmin";
}
