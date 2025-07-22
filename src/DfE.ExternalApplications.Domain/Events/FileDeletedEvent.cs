using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Events
{
    public sealed record FileDeletedEvent(
        FileId FileId, DateTime AddedOn) : IDomainEvent
    {
        public DateTime OccurredOn => AddedOn;
    }
}
