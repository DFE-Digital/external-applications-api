using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetUploadsByApplicationIdQueryObject(ApplicationId applicationId) : IQueryObject<Upload>
{
    public IQueryable<Upload> Apply(IQueryable<Upload> query) =>
        query.Where(u => u.ApplicationId == applicationId);
} 