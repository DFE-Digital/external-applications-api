using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class Permission : IEntity<PermissionId>
{
    public PermissionId? Id { get; private set; }
    public UserId UserId { get; private set; }
    public User? User { get; private set; }
    public ApplicationId ApplicationId { get; private set; }
    public Application? Application { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public string ResourceKey { get; private set; } = null!;
    public AccessType AccessType { get; private set; }
    public DateTime GrantedOn { get; private set; }
    public UserId GrantedBy { get; private set; }
    public User? GrantedByUser { get; private set; }

    /// <summary>
    /// Public constructor that initializes all required fields. 
    /// </summary>
    public Permission(
        PermissionId id,
        UserId userId,
        ApplicationId applicationId,
        string resourceKey,
        ResourceType resourceType,
        AccessType accessType,
        DateTime grantedOn,
        UserId grantedBy)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        UserId = userId;
        ApplicationId = applicationId;
        ResourceKey = resourceKey ?? throw new ArgumentNullException(nameof(resourceKey));
        ResourceType = resourceType;
        AccessType = accessType;
        GrantedOn = grantedOn;
        GrantedBy = grantedBy;
    }
}



