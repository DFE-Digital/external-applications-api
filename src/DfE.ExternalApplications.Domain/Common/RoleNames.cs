namespace DfE.ExternalApplications.Domain.Common;

/// <summary>
/// Well-known role names stored in the Roles table and issued as role claims.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
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
    /// Returns true when the role can be assigned through the administrative role assignment API.
    /// </summary>
    public static bool IsAssignable(string roleName) =>
        Assignable.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Resolves a role name to its canonical form, or null when not assignable.
    /// </summary>
    public static string? ResolveAssignable(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return null;

        return Assignable.FirstOrDefault(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }
}
