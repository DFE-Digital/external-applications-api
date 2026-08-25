using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Users.QueryObjects;

/// <summary>
/// Filters permission rows to application-scoped permissions for a user,
/// used by dashboard listing without loading the full permission graph.
/// </summary>
public sealed class GetApplicationIdsByUserIdQueryObject(UserId userId) : IQueryObject<Permission>
{
    /// <inheritdoc />
    public IQueryable<Permission> Apply(IQueryable<Permission> query) =>
        query.Where(p =>
            p.UserId == userId
            && p.ResourceType == ResourceType.Application
            && p.ApplicationId != null);
}
