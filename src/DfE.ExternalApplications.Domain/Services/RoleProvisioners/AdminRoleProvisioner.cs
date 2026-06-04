using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Services.RoleProvisioners;

/// <summary>
/// Provisions users with the Admin role for full tenant administration.
/// </summary>
public sealed class AdminRoleProvisioner(IUserFactory userFactory) : IUserRoleProvisioner
{
    /// <inheritdoc />
    public string RoleName => RoleNames.Admin;

    /// <inheritdoc />
    public bool RequiresTemplateIds => false;

    /// <inheritdoc />
    public User CreateUser(RoleAssignmentRequest request) =>
        userFactory.CreateAdmin(
            new UserId(Guid.NewGuid()),
            request.Name,
            request.Email,
            request.GrantedBy,
            request.GrantedOn);

    /// <inheritdoc />
    public void AssignToExistingUser(User user, RoleAssignmentRequest request) =>
        userFactory.GrantAdminAccess(user, request.GrantedBy, request.GrantedOn);
}
