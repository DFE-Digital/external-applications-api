using DfE.ExternalApplications.Application.Common.QueriesObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

/// <summary>
/// Filters applications by status.
/// </summary>
public sealed class GetApplicationsByStatusQueryObject(ApplicationStatus status)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => (a.Status ?? ApplicationStatus.Created) == status);
}
