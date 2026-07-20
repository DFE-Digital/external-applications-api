using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using File = GovUK.Dfe.FlexForms.Domain.Entities.File;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetFileByIdQueryObject(FileId fileId) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(u => u.Id != null && u.Id == fileId);
} 
