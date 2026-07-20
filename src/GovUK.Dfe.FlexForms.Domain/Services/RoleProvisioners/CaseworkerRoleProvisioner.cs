using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Factories;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Domain.Services.RoleProvisioners;

/// <summary>
/// Provisions users with the Caseworker role and read-only, template-scoped access.
/// </summary>
public sealed class CaseworkerRoleProvisioner(IUserFactory userFactory) : IUserRoleProvisioner
{
    /// <inheritdoc />
    public string RoleName => RoleNames.Caseworker;

    /// <inheritdoc />
    public bool RequiresTemplateIds => true;

    /// <inheritdoc />
    public User CreateUser(RoleAssignmentRequest request)
    {
        ValidateTemplateIds(request.TemplateIds);

        return userFactory.CreateCaseworker(
            new UserId(Guid.NewGuid()),
            request.Name,
            request.Email,
            request.TemplateIds,
            request.GrantedBy,
            request.GrantedOn);
    }

    /// <inheritdoc />
    public void AssignToExistingUser(User user, RoleAssignmentRequest request)
    {
        ValidateTemplateIds(request.TemplateIds);

        userFactory.GrantCaseworkerAccess(
            user,
            request.TemplateIds,
            request.GrantedBy,
            request.GrantedOn);
    }

    private static void ValidateTemplateIds(IReadOnlyCollection<TemplateId> templateIds)
    {
        if (templateIds.Count == 0)
            throw new ArgumentException("At least one template ID is required for the Caseworker role.", nameof(templateIds));
    }
}
