using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;

public sealed class GetLatestTemplateVersionForTemplateQueryObject(TemplateId templateId)
    : IQueryObject<TemplateVersion>
{
    public IQueryable<TemplateVersion> Apply(IQueryable<TemplateVersion> query) =>
        query
            .Where(tv => tv.TemplateId == templateId)
            .OrderByDescending(tv => tv.CreatedOn);
}
