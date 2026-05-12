using System.Security.Claims;
using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Application.TenantConfig.Queries;

/// <summary>
/// Returns the merged 'Shared' + target-specific configuration for the tenant the
/// authenticated principal is registered against.
/// The tenant is never client-specified; it is resolved from the JWT principal.
/// </summary>
public sealed record GetTenantConfigurationQuery(string Target)
    : IRequest<Result<TenantConfigurationDto>>;

public sealed class GetTenantConfigurationQueryHandler(
    IHttpContextAccessor httpContextAccessor,
    ITenantPrincipalResolver principalResolver,
    ITenantSettingsReader settingsReader)
    : IRequestHandler<GetTenantConfigurationQuery, Result<TenantConfigurationDto>>
{
    public async Task<Result<TenantConfigurationDto>> Handle(
        GetTenantConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return Result<TenantConfigurationDto>.Forbid("Not authenticated.");

        var principalObjectId = ExtractPrincipalObjectId(user);

        if (string.IsNullOrEmpty(principalObjectId))
            return Result<TenantConfigurationDto>.Forbid(
                "Could not determine principal id from token (expected 'oid' or 'appid' claim).");

        var resolution = await principalResolver.ResolveAsync(principalObjectId, cancellationToken);

        if (resolution is null)
            return Result<TenantConfigurationDto>.Forbid(
                $"Principal '{principalObjectId}' is not registered to any tenant.");

        var snapshot = await settingsReader.GetConfigurationAsync(
            resolution.TenantId,
            request.Target,
            cancellationToken);

        if (snapshot is null)
            return Result<TenantConfigurationDto>.NotFound(
                $"Tenant '{resolution.TenantId}' is not active or no longer exists.");

        return Result<TenantConfigurationDto>.Success(new TenantConfigurationDto(
            snapshot.TenantId,
            snapshot.TenantName,
            request.Target,
            snapshot.LoadedAtUtc,
            snapshot.Configuration));
    }

    /// <summary>
    /// Extracts the principal's stable object identifier from the JWT claims.
    /// Tries the standard AAD 'oid' claim first, then falls back to 'appid' (app-only tokens),
    /// then 'sub' as a last resort.
    /// </summary>
    private static string? ExtractPrincipalObjectId(ClaimsPrincipal user)
    {
        return user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
               ?? user.FindFirstValue("oid")
               ?? user.FindFirstValue("appid")
               ?? user.FindFirstValue("azp")
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
