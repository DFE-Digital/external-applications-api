using System.Diagnostics.CodeAnalysis;
using GovUK.Dfe.FlexForms.Domain.Common;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Infrastructure.Database;

namespace GovUK.Dfe.FlexForms.Infrastructure.Repositories
{
    [ExcludeFromCodeCoverage]
    public class EaRepository<TAggregate>(ExternalApplicationsContext dbContext)
        : Repository<TAggregate, ExternalApplicationsContext>(dbContext), IEaRepository<TAggregate>
        where TAggregate : class, IAggregateRoot
    {
    }
}