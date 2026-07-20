using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetApplicationsByTemplateIdQueryObject(TemplateId templateId, ApplicationStatus? status)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.TemplateVersion!.TemplateId == templateId && (!status.HasValue || a.Status == status))
             .Include(a => a.TemplateVersion)
             .ThenInclude(tv => tv!.Template);
} 
