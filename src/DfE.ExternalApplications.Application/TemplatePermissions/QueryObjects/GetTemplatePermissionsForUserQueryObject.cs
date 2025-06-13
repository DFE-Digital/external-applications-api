using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.TemplatePermissions.QueryObjects
{
    /// <summary>
    /// Filters to one user by normalized email, and includes all Template Permission children.
    /// </summary>
    public sealed class GetTemplatePermissionsForUserQueryObject(string email)
        : IQueryObject<User>
    {
        private readonly string _normalizedEmail = email.Trim().ToLowerInvariant();

        public IQueryable<User> Apply(IQueryable<User> query)
        {
            return query
                .Where(u => u.Email.ToLower() == _normalizedEmail)
                .Include(u => u.TemplatePermissions);
        }
    }
}