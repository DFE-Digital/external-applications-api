using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;

namespace DfE.ExternalApplications.Application.Services;

public class TemplatePermissionService(ISender mediator) : ITemplatePermissionService
{
    public async Task<bool> CanUserCreateApplicationForTemplate(string principalId, Guid templateId, CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyCollection<TemplatePermissionDto>> result;
        if (principalId.Contains('@'))
        {
            result = await mediator.Send(new GetTemplatePermissionsForUserQuery(principalId), cancellationToken);
        }
        else
        {
            result = await mediator.Send(new GetTemplatePermissionsForUserByExternalProviderIdQuery(principalId), cancellationToken);
        }

        if (!result.IsSuccess)
            return false;

        return result.Value!.Any(p => 
            p.TemplateId == templateId && 
            p.AccessType == AccessType.Write);
    }

    public async Task<bool> CanUserCreateApplicationForTemplate(UserId userId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var permissionsResult = await mediator.Send(
            new GetTemplatePermissionsForUserByUserIdQuery(userId),
            cancellationToken);

        if (!permissionsResult.IsSuccess)
            return false;

        return permissionsResult.Value!.Any(p => 
            p.TemplateId == templateId && 
            p.AccessType == AccessType.Write);
    }
} 