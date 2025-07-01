using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects
{
    public class GetUserWithAllPermissionsByExternalIdQueryObject(string externalProviderId)
    {
        public IQueryable<User> Apply(IQueryable<User> query)
        {
            if (string.IsNullOrWhiteSpace(externalProviderId))
                return query.Where(u => false); // Return empty result

            return query
                .Where(u => u.ExternalProviderId == externalProviderId)
                .Include(u => u.Permissions);
        }
    }
}
