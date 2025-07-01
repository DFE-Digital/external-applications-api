using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects
{
    /// <summary>
    /// Filters to one user by UserId, and includes all Permission children.
    /// </summary>
    public sealed class GetUserWithAllPermissionsByUserIdQueryObject(UserId userId)
        : IQueryObject<User>
    {
        public IQueryable<User> Apply(IQueryable<User> query)
        {
            return query
                .Where(u => u.Id == userId)
                .Include(u => u.Permissions);
        }
    }
}