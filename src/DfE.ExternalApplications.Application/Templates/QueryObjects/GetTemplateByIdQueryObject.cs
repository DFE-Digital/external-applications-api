using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

public sealed class GetTemplateByIdQueryObject(TemplateId templateId)
    : IQueryObject<Template>
{
    public IQueryable<Template> Apply(IQueryable<Template> query) =>
        query
            .Include(t => t.TemplateVersions)
            .Where(t => t.Id == templateId);
} 