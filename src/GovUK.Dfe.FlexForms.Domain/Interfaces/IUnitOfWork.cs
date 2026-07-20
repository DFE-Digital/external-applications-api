using System.Threading;
using System.Threading.Tasks;
namespace GovUK.Dfe.FlexForms.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
