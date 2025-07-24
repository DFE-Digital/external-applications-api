using DfE.ExternalApplications.Application.Common.QueriesObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetFileByFileNameApplicationIdQueryObject(string fileName, ApplicationId applicationId) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(u => u.ApplicationId == applicationId && u.FileName == fileName);
} 