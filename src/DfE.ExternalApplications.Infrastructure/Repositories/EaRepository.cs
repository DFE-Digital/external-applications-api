using System.Diagnostics.CodeAnalysis;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Infrastructure.Database;

namespace DfE.ExternalApplications.Infrastructure.Repositories
{
    [ExcludeFromCodeCoverage]
    public class EaRepository<TAggregate>(ExternalApplicationsContext dbContext)
        : Repository<TAggregate, ExternalApplicationsContext>(dbContext), IEaRepository<TAggregate>
        where TAggregate : class, IAggregateRoot
    {
    }
}