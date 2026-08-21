namespace DfE.ExternalApplications.Domain.Common;

/// <summary>
/// Well-known role names stored in the Roles table and issued as role claims.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";

    /// <summary>
    /// Database alias for <see cref="Admin"/>. Treated as Admin for authorization and claims.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    public const string User = "User";
    public const string Caseworker = "Caseworker";

    /// <summary>
    /// Roles that can be assigned through the administrative role assignment API.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Assignable =
    [
        User,
        Caseworker,
        Admin
    ];

    /// <summary>
    /// Returns true when the role name represents full administrative access
    /// (<see cref="Admin"/> or <see cref="SuperAdmin"/>).
    /// </summary>
    public static bool IsAdminName(string? roleName) =>
        string.Equals(roleName, Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Maps a database role name to the canonical claim/authorization role.
    /// <see cref="SuperAdmin"/> is emitted as <see cref="Admin"/>.
    /// </summary>
    public static string ToClaimRole(string? dbRoleName)
    {
        if (string.IsNullOrWhiteSpace(dbRoleName))
            return string.Empty;

        return IsAdminName(dbRoleName) ? Admin : dbRoleName;
    }

    /// <summary>
    /// Returns true when the role can be assigned through the administrative role assignment API.
    /// </summary>
    public static bool IsAssignable(string roleName) =>
        ResolveAssignable(roleName) is not null;

    /// <summary>
    /// Resolves a role name to its canonical form, or null when not assignable.
    /// <see cref="SuperAdmin"/> resolves to <see cref="Admin"/>.
    /// </summary>
    public static string? ResolveAssignable(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return null;

        if (IsAdminName(roleName))
            return Admin;

        return Assignable.FirstOrDefault(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns true when assigning <paramref name="targetRole"/> would downgrade an existing
    /// <paramref name="currentRole"/> to User. User is the lowest role; Admin and Caseworker
    /// cannot be reassigned to User.
    /// </summary>
    public static bool IsDowngradeToUser(string? currentRole, string targetRole)
    {
        if (!string.Equals(targetRole, User, StringComparison.OrdinalIgnoreCase))
            return false;

        var current = IsAdminName(currentRole)
            ? Admin
            : ResolveAssignable(currentRole ?? string.Empty);

        if (current is null || string.Equals(current, User, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Resolves a role name from a well-known role identifier.
    /// </summary>
    public static string? FromRoleId(Guid roleId)
    {
        if (roleId == RoleConstants.AdminRoleId)
            return Admin;

        if (roleId == RoleConstants.CaseworkerRoleId)
            return Caseworker;

        if (roleId == RoleConstants.UserRoleId)
            return User;

        return null;
    }
}
