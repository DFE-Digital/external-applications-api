using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;

/// <summary>
/// Projects all non-null template IDs from the tenant templates table.
/// </summary>
public sealed class GetAllTemplateIdsQueryObject
{
    /// <summary>
    /// Returns template IDs present in the database.
    /// </summary>
    public IQueryable<TemplateId> Apply(IQueryable<Template> query) =>
        query
            .Where(t => t.Id != null)
            .Select(t => t.Id!);
}
