using DfE.ExternalApplications.Application.Common.QueriesObjects;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetApplicationsByIdsQueryObject(IEnumerable<ApplicationId> ids)
    : IQueryObject<Domain.Entities.Application>
{
    private readonly HashSet<ApplicationId> _ids = ids.ToHashSet();

    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.Id != null && _ids.Contains(a.Id))
        .Include(a => a.TemplateVersion)
        .ThenInclude(a => a!.Template);
}