using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using Microsoft.EntityFrameworkCore;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetApplicationsByIdsQueryObject(IEnumerable<ApplicationId> ids)
    : IQueryObject<Domain.Entities.Application>
{
    private readonly HashSet<ApplicationId> _ids = ids.ToHashSet();

    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.Id != null && _ids.Contains(a.Id))
        .Include(a => a.TemplateVersion)
        .ThenInclude(a => a!.Template);
}
