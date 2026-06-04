using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

/// <summary>
/// Returns the most recent response for a single application using an indexed ApplicationId lookup.
/// </summary>
public sealed class GetLatestApplicationResponseByApplicationIdQueryObject(ApplicationId applicationId)
{
    public IQueryable<ApplicationResponse> Apply(IQueryable<ApplicationResponse> query) =>
        query
            .AsNoTracking()
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.CreatedOn)
            .Take(1);
}
