using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

public sealed class GetTemplatePermissionByTemplateNameQueryObject(string email, Guid templateId)
    : IQueryObject<TemplatePermission>
{
    private readonly string _normalizedEmail = email.Trim().ToLowerInvariant();

    public IQueryable<TemplatePermission> Apply(IQueryable<TemplatePermission> query) =>
        query
            .Include(x => x.Template)
            .Include(x => x.User)
            .Where(x => 
                x.User != null
                && x.User.Email.ToLower() == _normalizedEmail
                && x.Template!.Id == new TemplateId(templateId));
}
