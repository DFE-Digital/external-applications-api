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
    private readonly HashSet<TemplateId> _templateIds = templateIds.ToHashSet();

    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query)
    {
        if (_templateIds.Count == 0)
            return query.Where(_ => false);

        return query
            .Where(a => a.TemplateVersion != null && _templateIds.Contains(a.TemplateVersion.TemplateId))
            .Include(a => a.TemplateVersion)
            .ThenInclude(tv => tv!.Template);
    }
}
