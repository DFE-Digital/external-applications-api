using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Events;

public sealed record ApplicationCreatedEvent(
    ApplicationId ApplicationId,
    string ApplicationReference,
    TemplateVersionId TemplateVersionId,
    UserId CreatedBy,
    DateTime CreatedOn) : IDomainEvent
{
    public DateTime OccurredOn => CreatedOn;
} 