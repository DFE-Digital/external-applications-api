using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

public sealed class GetLatestTemplateVersionForTemplateQueryObject(TemplateId templateId)
    : IQueryObject<TemplateVersion>
{
    public IQueryable<TemplateVersion> Apply(IQueryable<TemplateVersion> query) =>
        query
            .Where(tv => tv.TemplateId == templateId)
            .OrderByDescending(tv => tv.CreatedOn)
            .Take(1);
}
