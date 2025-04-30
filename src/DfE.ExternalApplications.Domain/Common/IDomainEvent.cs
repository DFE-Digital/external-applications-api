using MediatR;

namespace DfE.ExternalApplications.Domain.Common
{
    public interface IDomainEvent : INotification
    {
        DateTime OccurredOn { get; }
    }
}
