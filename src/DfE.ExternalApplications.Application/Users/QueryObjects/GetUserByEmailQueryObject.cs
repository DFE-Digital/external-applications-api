using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects
{
    public class GetUserByEmailQueryObject(string email) : IQueryObject<User>
    {
        private readonly string _email = email.Trim().ToLowerInvariant();

        public IQueryable<User> Apply(IQueryable<User> query) =>
            query
                .Include(u => u.Role)
                .Where(u => u.Email.ToLower() == _email);
    }
}