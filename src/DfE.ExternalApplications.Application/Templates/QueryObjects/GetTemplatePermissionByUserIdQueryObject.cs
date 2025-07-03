using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

public sealed class GetTemplatePermissionByUserIdQueryObject(UserId userId, Guid templateId)
    : IQueryObject<TemplatePermission>
{
    public IQueryable<TemplatePermission> Apply(IQueryable<TemplatePermission> query) =>
        query
            .Include(x => x.Template)
            .Include(x => x.User)
            .Where(x => 
                x.User != null
                && x.User.Id == userId
                && x.Template!.Id == new TemplateId(templateId));
} 