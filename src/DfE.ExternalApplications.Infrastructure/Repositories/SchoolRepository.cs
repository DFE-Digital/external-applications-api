using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using DfE.ExternalApplications.Domain.Entities.Schools;

namespace DfE.ExternalApplications.Infrastructure.Repositories
{
    /// <summary>
    /// Please note this is an example of a specific repository for the aggregate root
    /// the following methods and queries can be achieved by Generic Repository's Find and Fetch methods 
    /// </summary>
    /// <param name="context"></param>
    public class SchoolRepository(ExternalApplicationsContext context) : ISchoolRepository
    {
        public async Task<School?> GetPrincipalBySchoolAsync(string schoolName, CancellationToken cancellationToken)
        {
            return new School();
        }

        public IQueryable<School> GetPrincipalsBySchoolsQueryable(List<string> schoolNames)
        {
            return (IQueryable<School>)context.Applications;

        }
    }
}
