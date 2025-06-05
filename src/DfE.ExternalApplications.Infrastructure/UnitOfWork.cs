using System.Threading;
using System.Threading.Tasks;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Infrastructure.Database;

namespace DfE.ExternalApplications.Infrastructure;

public class UnitOfWork(ExternalApplicationsContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
