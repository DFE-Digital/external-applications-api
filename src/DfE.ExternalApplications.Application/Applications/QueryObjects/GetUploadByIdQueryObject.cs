using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetUploadByIdQueryObject(Guid fileId) : IQueryObject<Upload>
{
    public IQueryable<Upload> Apply(IQueryable<Upload> query) =>
        query.Where(u => u.Id != null && u.Id.Value == fileId);
} 