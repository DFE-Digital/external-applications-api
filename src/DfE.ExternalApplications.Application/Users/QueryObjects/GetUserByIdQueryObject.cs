using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects
{
    public class GetUserByIdQueryObject(UserId userId) : IQueryObject<User>
    {
        public IQueryable<User> Apply(IQueryable<User> query) =>
            query
                .Include(u => u.Role)
                .Where(u => u.Id == userId);
    }
}