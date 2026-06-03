using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Provisions or updates a user for a specific assignable role.
/// </summary>
public interface IUserRoleProvisioner
{
    /// <summary>
    /// The canonical role name this provisioner handles.
    /// </summary>
    string RoleName { get; }

    /// <summary>
    /// Whether at least one template ID is required when assigning this role.
    /// </summary>
    bool RequiresTemplateIds { get; }

    /// <summary>
    /// Creates a new user with the role and default permissions for that role.
    /// </summary>
    User CreateUser(RoleAssignmentRequest request);

    /// <summary>
    /// Assigns the role and default permissions to an existing user.
    /// </summary>
    void AssignToExistingUser(User user, RoleAssignmentRequest request);
}

/// <summary>
/// Input for provisioning a user with an assignable role.
/// </summary>
public sealed record RoleAssignmentRequest(
    string Name,
    string Email,
    IReadOnlyCollection<TemplateId> TemplateIds,
    UserId GrantedBy,
    DateTime GrantedOn);
