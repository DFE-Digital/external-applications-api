using GovUK.Dfe.FlexForms.Domain.Common;

namespace GovUK.Dfe.FlexForms.Domain.Services;

/// <summary>
/// Resolves role provisioners for assignable roles.
/// </summary>
public interface IUserRoleProvisionerRegistry
{
    /// <summary>
    /// Gets the provisioner for the specified assignable role name.
    /// </summary>
    IUserRoleProvisioner? GetProvisioner(string roleName);
}

/// <inheritdoc />
public sealed class UserRoleProvisionerRegistry(IEnumerable<IUserRoleProvisioner> provisioners)
    : IUserRoleProvisionerRegistry
{
    private readonly IReadOnlyDictionary<string, IUserRoleProvisioner> _provisioners =
        provisioners.ToDictionary(p => p.RoleName, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IUserRoleProvisioner? GetProvisioner(string roleName)
    {
        var canonical = RoleNames.ResolveAssignable(roleName);
        return canonical is null ? null : _provisioners.GetValueOrDefault(canonical);
    }
}
