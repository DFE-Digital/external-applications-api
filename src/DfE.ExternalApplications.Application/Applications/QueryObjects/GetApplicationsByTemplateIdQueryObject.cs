using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetApplicationsByTemplateIdQueryObject(TemplateId templateId)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.TemplateVersion!.TemplateId == templateId)
             .Include(a => a.TemplateVersion)
             .ThenInclude(tv => tv!.Template);
} 