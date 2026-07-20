using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;

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
