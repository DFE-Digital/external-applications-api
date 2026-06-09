using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetApplicationsByTemplateIdQueryObject(TemplateId templateId, ApplicationStatus? status)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.TemplateVersion!.TemplateId == templateId && (!status.HasValue || a.Status == status))
             .Include(a => a.TemplateVersion)
             .ThenInclude(tv => tv!.Template);
} 