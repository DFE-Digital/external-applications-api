using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.QueryObjects;

public sealed class GetUserByExternalProviderIdQueryObject(string externalProviderId) : IQueryObject<User>
{
    public IQueryable<User> Apply(IQueryable<User> query) =>
        string.IsNullOrWhiteSpace(externalProviderId)
            ? query.Where(u => false) // Return empty result set when externalProviderId is null/empty
            : query
                .Include(u => u.Role)
                .Where(u => u.ExternalProviderId == externalProviderId);
} 