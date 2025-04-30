using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities.Schools;

namespace DfE.ExternalApplications.Domain.Events
{
    public class SchoolCreatedEvent(School school) : IDomainEvent
    {
        public School School { get; } = school;

        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
