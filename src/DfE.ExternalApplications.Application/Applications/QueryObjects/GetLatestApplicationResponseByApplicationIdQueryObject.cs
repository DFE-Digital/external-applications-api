using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed partial class GetApplicationByReferenceDtoQueryObject
{
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
}


