using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.TemplatePermissions.QueryObjects;

public sealed class GetTemplatePermissionsForUserByUserIdQueryObject(UserId userId)
    : IQueryObject<User>
{
    public IQueryable<User> Apply(IQueryable<User> query) =>
        query
            .Where(u => u.Id == userId)
            .Include(u => u.TemplatePermissions);
} 
