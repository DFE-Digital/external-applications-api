using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Events;

public sealed record ApplicationResponseAddedEvent(
    ApplicationId ApplicationId,
    ResponseId ResponseId,
    UserId AddedBy,
    DateTime AddedOn) : IDomainEvent
{
    public DateTime OccurredOn => AddedOn;
} 