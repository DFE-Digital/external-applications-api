using DfE.ExternalApplications.Application.Common.QueriesObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public class GetApplicationsByStatusQueryObject(ApplicationStatus status)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query)
    {
        return query.Where(x => x.Status == status);
    }
}
