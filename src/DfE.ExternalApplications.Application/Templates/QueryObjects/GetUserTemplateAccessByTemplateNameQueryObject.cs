using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

public sealed class GetTemplatePermissionByTemplateNameQueryObject(string email, string templateName)
    : IQueryObject<TemplatePermission>
{
    private readonly string _normalizedEmail = email.Trim().ToLowerInvariant();
    private readonly string _normalizedName = templateName.Trim().ToLowerInvariant();

    public IQueryable<TemplatePermission> Apply(IQueryable<TemplatePermission> query) =>
        query
            .Include(x => x.Template)
            .Include(x => x.User)
            .Where(x => 
                x.User != null
                && x.User.Email.Equals(_normalizedEmail, StringComparison.InvariantCultureIgnoreCase)
                && x.Template!.Name.ToLower() == _normalizedName);
}
