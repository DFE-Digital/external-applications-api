using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;

namespace DfE.ExternalApplications.Application.Users.QueryObjects
{
    public class GetUserByEmailQueryObject(string email) : IQueryObject<User>
    {
        private readonly string _email = email.Trim().ToLowerInvariant();

        public IQueryable<User> Apply(IQueryable<User> query) =>
            query.Where(u => u.Email.ToLower() == _email);
    }
}
