using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

public sealed class GetContributorsForApplicationQueryObject(ApplicationId applicationId)
    : IQueryObject<User>
{
    public IQueryable<User> Apply(IQueryable<User> query) =>
        query
            .Include(u => u.Permissions)
            .Include(u => u.Role)
            .Where(u => u.Permissions.Any(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.Application))
            .Where(u => u.Permissions.Any(p => p.ApplicationId == applicationId && p.Application!.CreatedBy != u.Id));
} 