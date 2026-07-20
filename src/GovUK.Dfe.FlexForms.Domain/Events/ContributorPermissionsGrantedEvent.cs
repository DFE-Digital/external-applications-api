using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Events;

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
