using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Events;

public sealed record ContributorPermissionsGrantedEvent(
    ApplicationId ApplicationId,
    string ApplicationReference,
    TemplateId TemplateId,
    User Contributor,
    AccessType[] GrantedAccessTypes,
    UserId GrantedBy,
    DateTime GrantedOn) : IDomainEvent
{
    public DateTime OccurredOn => GrantedOn;
}
