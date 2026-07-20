using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using File = GovUK.Dfe.FlexForms.Domain.Entities.File;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetFileByPathAndFileNameQueryObject(string path, string fileName) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(f => f.Path == path && f.FileName == fileName);
}
