using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Events;

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
