using DfE.ExternalApplications.Domain.Entities.Schools;

namespace DfE.ExternalApplications.Domain.Interfaces.Repositories
{
    public interface ISchoolRepository
    {
        Task<School?> GetPrincipalBySchoolAsync(string schoolName, CancellationToken cancellationToken);
        IQueryable<School> GetPrincipalsBySchoolsQueryable(List<string> schoolNames);

    }
}
