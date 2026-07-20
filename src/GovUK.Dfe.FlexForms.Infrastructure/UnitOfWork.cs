using System.Threading;
using System.Threading.Tasks;
using GovUK.Dfe.FlexForms.Domain.Interfaces;
using GovUK.Dfe.FlexForms.Infrastructure.Database;

namespace GovUK.Dfe.FlexForms.Infrastructure;

public class UnitOfWork(ExternalApplicationsContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
