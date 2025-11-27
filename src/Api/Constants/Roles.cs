namespace Api.Constants
{
    /// <summary>
    /// Application role constants for role-based access control
    /// </summary>
    public static class AppRoles
    {
        /// <summary>
        /// System Administrators - Full access to all resources including user management, roles, and destructive operations
        /// </summary>
        public const string SystemAdministrator = "SystemAdministrators";

        /// <summary>
        /// General Users - Read access to most resources, limited create/update capabilities, no access to admin functions
        /// </summary>
        public const string GeneralUser = "GeneralUsers";

        /// <summary>
        /// Combined roles for authorization - any user (admin or general)
        /// </summary>
        public const string AnyUser = SystemAdministrator + "," + GeneralUser;
    }
}
