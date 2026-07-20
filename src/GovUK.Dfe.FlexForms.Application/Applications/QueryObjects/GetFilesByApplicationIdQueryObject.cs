using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;
using File = GovUK.Dfe.FlexForms.Domain.Entities.File;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetFilesByApplicationIdQueryObject(ApplicationId applicationId) : IQueryObject<File>
{
    public IQueryable<File> Apply(IQueryable<File> query) =>
        query.Where(u => u.ApplicationId == applicationId);
} 
