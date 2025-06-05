using System.Threading;
using System.Threading.Tasks;
namespace DfE.ExternalApplications.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
