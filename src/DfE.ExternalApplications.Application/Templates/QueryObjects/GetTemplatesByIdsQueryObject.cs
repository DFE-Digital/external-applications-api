using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

/// <summary>
/// Loads templates (with versions) whose IDs are in the supplied set, ordered by name.
/// </summary>
public sealed class GetTemplatesByIdsQueryObject(IEnumerable<TemplateId> templateIds)
    : IQueryObject<Template>
{
    private readonly HashSet<TemplateId> _templateIds = templateIds.ToHashSet();

    public IQueryable<Template> Apply(IQueryable<Template> query)
    {
        if (_templateIds.Count == 0)
        {
            return query.Where(_ => false);
        }

        // Compare TemplateId value objects (not .Value) so EF can translate Contains to SQL IN.
        return query
            .Include(t => t.TemplateVersions)
            .Where(t => t.Id != null && _templateIds.Contains(t.Id))
            .OrderBy(t => t.Name);
    }
}
