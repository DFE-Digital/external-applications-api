using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects;

/// <summary>
/// Filters to one user by normalized email and includes role, permissions, and template permissions.
/// </summary>
public sealed class GetUserWithAllPermissionsByEmailQueryObject(string email) : IQueryObject<User>
{
    private readonly string _normalizedEmail = email.Trim().ToLowerInvariant();

    /// <inheritdoc />
    public IQueryable<User> Apply(IQueryable<User> query) =>
        query
            .Where(u => u.Email.ToLower() == _normalizedEmail)
            .Include(u => u.Permissions)
            .Include(u => u.TemplatePermissions)
            .Include(u => u.Role);
}
