using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

/// <summary>
/// Loads templates (with versions) whose IDs are in the supplied set, ordered by name.
/// </summary>
public sealed class GetTemplatesByIdsQueryObject(IReadOnlyCollection<Guid> templateIds)
    : IQueryObject<Template>
{
    public IQueryable<Template> Apply(IQueryable<Template> query)
    {
        if (templateIds is null || templateIds.Count == 0)
        {
            return query.Where(_ => false);
        }

        return query
            .Include(t => t.TemplateVersions)
            .Where(t => t.Id != null && templateIds.Contains(t.Id.Value))
            .OrderBy(t => t.Name);
    }
}
