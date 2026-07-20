using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Domain.Events;

public sealed record UserCreatedEvent(
    User User,
    DateTime CreatedOn) : IDomainEvent
{
    public DateTime OccurredOn => CreatedOn;
}

