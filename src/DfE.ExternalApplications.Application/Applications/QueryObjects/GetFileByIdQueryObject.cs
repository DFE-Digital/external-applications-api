using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetFileByIdQueryObject(FileId fileId) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(u => u.Id != null && u.Id == fileId);
} 