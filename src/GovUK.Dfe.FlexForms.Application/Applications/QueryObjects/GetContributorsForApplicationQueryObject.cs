using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

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
