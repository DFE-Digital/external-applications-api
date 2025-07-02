using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects
{
    public sealed class GetUserWithAllTemplatePermissionsByExternalIdQueryObject(string externalProviderId)
    {
        public IQueryable<User> Apply(IQueryable<User> query) =>
            query
                .Where(u => u.ExternalProviderId == externalProviderId)
                .Include(u => u.TemplatePermissions);
    }
}
