using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Events;

public sealed record ApplicationSubmittedEvent(
    ApplicationId ApplicationId,
    string ApplicationReference,
    TemplateId TemplateId,
    UserId SubmittedBy,
    string UserEmail,
    string UserFullName,
    DateTime SubmittedOn) : IDomainEvent
{
    public DateTime OccurredOn => SubmittedOn;
}
