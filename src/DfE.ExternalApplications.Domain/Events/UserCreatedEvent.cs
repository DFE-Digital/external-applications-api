using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Events;

public sealed record UserCreatedEvent(
    User User,
    DateTime CreatedOn) : IDomainEvent
{
    public DateTime OccurredOn => CreatedOn;
}

