using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetApplicationByReferenceQueryObject(string applicationReference)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.ApplicationReference == applicationReference)
             .Include(a => a.TemplateVersion)
             .ThenInclude(tv => tv!.Template)
             .Include(a => a.CreatedByUser);
}
