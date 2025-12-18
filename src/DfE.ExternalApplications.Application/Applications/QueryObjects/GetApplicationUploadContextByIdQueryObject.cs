using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed record ApplicationUploadReadModel(string ApplicationReference, string? LatestResponseBody);

/// <summary>
/// Optimized query for upload flows:
/// - Loads ApplicationReference
/// - Loads only the latest response body (not full response history)
/// </summary>
public sealed class GetApplicationUploadContextByIdQueryObject(ApplicationId applicationId)
{
    public IQueryable<ApplicationUploadReadModel> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new ApplicationUploadReadModel(
                a.ApplicationReference,
                a.Responses
                    .OrderByDescending(r => r.CreatedOn)
                    .Select(r => r.ResponseBody)
                    .FirstOrDefault()
            ));
}


