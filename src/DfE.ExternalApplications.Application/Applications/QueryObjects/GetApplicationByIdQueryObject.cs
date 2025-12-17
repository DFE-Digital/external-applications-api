using DfE.ExternalApplications.Application.Common.QueriesObjects;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetApplicationByIdQueryObject(ApplicationId applicationId)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.Id == applicationId)
             .Include(a => a.TemplateVersion)
             .ThenInclude(tv => tv!.Template);
} 