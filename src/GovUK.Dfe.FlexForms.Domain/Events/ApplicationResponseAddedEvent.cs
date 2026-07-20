using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Events;

public sealed record ApplicationResponseAddedEvent(
    ApplicationId ApplicationId,
    ResponseId ResponseId,
    UserId AddedBy,
    DateTime AddedOn) : IDomainEvent
{
    public DateTime OccurredOn => AddedOn;
} 
