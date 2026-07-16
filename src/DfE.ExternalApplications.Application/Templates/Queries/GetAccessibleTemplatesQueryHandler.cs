using System.Security.Claims;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.Queries;

/// <summary>
/// Returns templates in the current tenant that the caller is allowed to access.
/// Admins receive the full tenant catalogue; other users receive catalogue ∩ permissions.
/// </summary>
public sealed record GetAccessibleTemplatesQuery
    : IRequest<Result<IReadOnlyCollection<TemplateDto>>>;

/// <summary>
/// Handles <see cref="GetAccessibleTemplatesQuery"/>.
/// </summary>
public sealed class GetAccessibleTemplatesQueryHandler(
    IEaRepository<Template> templateRepository,
    IEaRepository<User> userRepository,
    IHttpContextAccessor httpContextAccessor,
    ITenantTemplateCatalogue tenantTemplateCatalogue,
    IUserAccessibleTemplateService userAccessibleTemplateService,
    IPermissionCheckerService permissionCheckerService)
    : IRequestHandler<GetAccessibleTemplatesQuery, Result<IReadOnlyCollection<TemplateDto>>>
{
    public async Task<Result<IReadOnlyCollection<TemplateDto>>> Handle(
        GetAccessibleTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<TemplateId> accessibleIds;

            if (permissionCheckerService.IsAdmin())
            {
                accessibleIds = await tenantTemplateCatalogue.GetTemplateIdsAsync(cancellationToken);
            }
            else
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext?.User is not ClaimsPrincipal principal || principal.Identity?.IsAuthenticated != true)
                {
                    return Result<IReadOnlyCollection<TemplateDto>>.Forbid("Not authenticated");
                }

                var principalId = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("appid")
                    ?? principal.FindFirstValue("azp");

                if (string.IsNullOrWhiteSpace(principalId))
                {
                    return Result<IReadOnlyCollection<TemplateDto>>.Forbid("No user identifier");
                }

                User? dbUser;
                if (principalId.Contains('@'))
                {
                    dbUser = await new GetUserWithAllTemplatePermissionsQueryObject(principalId)
                        .Apply(userRepository.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    dbUser = await new GetUserWithAllTemplatePermissionsByExternalIdQueryObject(principalId)
                        .Apply(userRepository.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (dbUser is null)
                {
                    return Result<IReadOnlyCollection<TemplateDto>>.NotFound("User not found");
                }

                accessibleIds = await userAccessibleTemplateService.GetAccessibleTemplateIdsAsync(
                    dbUser.TemplatePermissions,
                    cancellationToken);
            }

            if (accessibleIds.Count == 0)
            {
                return Result<IReadOnlyCollection<TemplateDto>>.Success(Array.Empty<TemplateDto>());
            }

            var accessibleValues = accessibleIds.Select(id => id.Value).ToList();

            var templates = await new GetTemplatesByIdsQueryObject(accessibleValues)
                .Apply(templateRepository.Query().AsNoTracking())
                .ToListAsync(cancellationToken);

            // HostMappings-only GUIDs with no DB row are omitted from detail listing.
            var dtos = templates
                .Select(t => new TemplateDto
                {
                    TemplateId = t.Id!.Value,
                    Name = t.Name,
                    CreatedOn = t.CreatedOn,
                    LatestVersionNumber = t.TemplateVersions
                        .OrderByDescending(v => v.CreatedOn)
                        .Select(v => v.VersionNumber)
                        .FirstOrDefault()
                })
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyCollection<TemplateDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyCollection<TemplateDto>>.Failure(ex.ToString());
        }
    }
}
