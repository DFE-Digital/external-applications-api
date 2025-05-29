using DfE.ExternalApplications.Domain.Common;

namespace DfE.ExternalApplications.Domain.Interfaces.Repositories
{
    public interface IEaRepository<TAggregate> : IRepository<TAggregate>
        where TAggregate : class, IAggregateRoot
    {
    }
}
