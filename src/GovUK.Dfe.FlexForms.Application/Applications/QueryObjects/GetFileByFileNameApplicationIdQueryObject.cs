using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;
using File = GovUK.Dfe.FlexForms.Domain.Entities.File;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetFileByFileNameApplicationIdQueryObject(string fileName, ApplicationId applicationId) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(u => u.ApplicationId == applicationId && u.FileName == fileName);
} 
