using GovUK.Dfe.FlexForms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

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
