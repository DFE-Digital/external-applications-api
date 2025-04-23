using System.Diagnostics.CodeAnalysis;
using ExternalApplications.Domain.Common;
using ExternalApplications.Domain.Interfaces.Repositories;
using ExternalApplications.Infrastructure.Database;

namespace ExternalApplications.Infrastructure.Repositories
{
    [ExcludeFromCodeCoverage]
    public class SclRepository<TAggregate>(SclContext dbContext)
        : Repository<TAggregate, SclContext>(dbContext), ISclRepository<TAggregate>
        where TAggregate : class, IAggregateRoot
    {
    }
}