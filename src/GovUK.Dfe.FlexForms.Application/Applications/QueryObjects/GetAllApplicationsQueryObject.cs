using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

/// <summary>
/// Returns all applications in the tenant with template navigation properties included.
/// </summary>
public sealed class GetAllApplicationsQueryObject : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Include(a => a.TemplateVersion)
            .ThenInclude(tv => tv!.Template);
}
