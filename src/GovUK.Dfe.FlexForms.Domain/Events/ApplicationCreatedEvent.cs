using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Events;

public sealed record ApplicationCreatedEvent(
    ApplicationId ApplicationId,
    string ApplicationReference,
    TemplateVersionId TemplateVersionId,
    UserId CreatedBy,
    DateTime CreatedOn) : IDomainEvent
{
    public DateTime OccurredOn => CreatedOn;
} 
