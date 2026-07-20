using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;

public sealed class GetTemplateByIdQueryObject(TemplateId templateId)
    : IQueryObject<Template>
{
    public IQueryable<Template> Apply(IQueryable<Template> query) =>
        query
            .Include(t => t.TemplateVersions)
            .Where(t => t.Id == templateId);
} 
