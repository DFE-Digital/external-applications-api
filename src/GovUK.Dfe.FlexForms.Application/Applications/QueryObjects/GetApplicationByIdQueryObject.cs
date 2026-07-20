using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using Microsoft.EntityFrameworkCore;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetApplicationByIdQueryObject(ApplicationId applicationId)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.Id == applicationId)
             .Include(a => a.TemplateVersion)
             .ThenInclude(tv => tv!.Template);
} 
