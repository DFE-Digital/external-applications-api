using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Factories;

public interface IUserFactory
{
    User CreateContributor(
        UserId id,
        RoleId roleId,
        string name,
        string email,
        UserId createdBy,
        ApplicationId applicationId,
        string applicationReference,
        TemplateId templateId,
        DateTime? createdOn = null);

    User CreateUser(
        UserId id,
        RoleId roleId,
        string name,
        string email,
        TemplateId templateId,
        DateTime? createdOn = null);

    void AddPermissionToUser(
        User user,
        string resourceKey,
        ResourceType resourceType,
        AccessType[] accessTypes,
        UserId grantedBy,
        ApplicationId? applicationId = null,
        DateTime? grantedOn = null);

    void AddTemplatePermissionToUser(
        User user,
        string templateId,
        AccessType[] accessTypes,
        UserId grantedBy,
        DateTime? grantedOn = null);

    /// <summary>
    /// Ensures the user has Read and Write template permission for the given template (idempotent).
    /// Call from registration or other flows when a user must have access to a template.
    /// </summary>
    void EnsureUserHasTemplatePermission(
        User user,
        TemplateId templateId,
        UserId grantedBy,
        DateTime? grantedOn = null);

    bool RemovePermissionFromUser(
        User user,
        Permission permission);
} 