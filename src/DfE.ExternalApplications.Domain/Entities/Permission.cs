using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class Permission : BaseAggregateRoot, IEntity<PermissionId>
{
    public PermissionId? Id { get; private set; }
    public UserId UserId { get; private set; }
    public User? User { get; private set; }
    public ApplicationId ApplicationId { get; private set; }
    public Application? Application { get; private set; }
    public string ResourceKey { get; private set; } = null!;
    public byte AccessType { get; private set; }
    public DateTime GrantedOn { get; private set; }
    public UserId GrantedBy { get; private set; }
    public User? GrantedByUser { get; private set; }
}