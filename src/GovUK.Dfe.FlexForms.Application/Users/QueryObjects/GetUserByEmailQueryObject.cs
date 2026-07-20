using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Users.QueryObjects
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
