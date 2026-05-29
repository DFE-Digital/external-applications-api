using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

/// <summary>
/// Filters applications to those whose template belongs to one of the specified template IDs.
/// </summary>
public sealed class GetApplicationsByTemplateIdsQueryObject(IEnumerable<TemplateId> templateIds)
    : IQueryObject<Domain.Entities.Application>
{
    private readonly HashSet<Guid> _templateIdValues = templateIds.Select(t => t.Value).ToHashSet();

    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.TemplateVersion != null && _templateIdValues.Contains(a.TemplateVersion.TemplateId.Value))
            .Include(a => a.TemplateVersion)
            .ThenInclude(tv => tv!.Template);
}
