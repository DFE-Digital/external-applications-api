using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Domain.Events
{
    public sealed record FileDeletedEvent(
        FileId FileId, DateTime AddedOn) : IDomainEvent
    {
        public DateTime OccurredOn => AddedOn;
    }
}
