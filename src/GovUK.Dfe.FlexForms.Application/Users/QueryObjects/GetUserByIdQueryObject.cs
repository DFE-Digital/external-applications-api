using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Users.QueryObjects
{
    public class GetUserByIdQueryObject(UserId userId) : IQueryObject<User>
    {
        public IQueryable<User> Apply(IQueryable<User> query) =>
            query
                .Include(u => u.Role)
                .Where(u => u.Id == userId);
    }
}
