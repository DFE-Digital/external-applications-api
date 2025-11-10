using DfE.ExternalApplications.Application.Common.QueriesObjects;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetFileByPathAndFileNameQueryObject(string path, string fileName) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(f => f.Path == path && f.FileName == fileName);
}