using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

public sealed class GetUserTemplateAccessByTemplateNameQueryObject(Guid userId, string templateName)
    : IQueryObject<UserTemplateAccess>
{
    private readonly UserId _userId = new(userId);
    private readonly string _normalizedName = templateName.Trim().ToLowerInvariant();

    public IQueryable<UserTemplateAccess> Apply(IQueryable<UserTemplateAccess> query) =>
        query
            .Include(x => x.Template)
            .Where(x => x.UserId == _userId && x.Template!.Name.ToLower() == _normalizedName);
}
