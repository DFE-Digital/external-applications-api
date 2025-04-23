using ExternalApplications.Domain.Common;
using ExternalApplications.Domain.Entities.Schools;

namespace ExternalApplications.Domain.Events
{
    public class SchoolCreatedEvent(School school) : IDomainEvent
    {
        public School School { get; } = school;

        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
