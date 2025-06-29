using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;

namespace DfE.ExternalApplications.Application.Users.QueryObjects;

public sealed class GetUserByExternalProviderIdQueryObject(string externalProviderId) : IQueryObject<User>
{
    public IQueryable<User> Apply(IQueryable<User> query) =>
        query.Where(u => u.ExternalProviderId == externalProviderId);
} 