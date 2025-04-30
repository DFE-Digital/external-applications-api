using System.Diagnostics.CodeAnalysis;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Infrastructure.Database;

namespace DfE.ExternalApplications.Infrastructure.Repositories
{
    [ExcludeFromCodeCoverage]
    public class SclRepository<TAggregate>(SclContext dbContext)
        : Repository<TAggregate, SclContext>(dbContext), ISclRepository<TAggregate>
        where TAggregate : class, IAggregateRoot
    {
    }
}