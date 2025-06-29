using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.TemplatePermissions.QueryObjects;

public sealed class GetTemplatePermissionsForUserByUserIdQueryObject(UserId userId)
    : IQueryObject<User>
{
    public IQueryable<User> Apply(IQueryable<User> query) =>
        query
            .Where(u => u.Id == userId)
            .Include(u => u.TemplatePermissions);
} 