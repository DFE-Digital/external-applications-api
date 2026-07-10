using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.TenantConfig.Queries;

/// <summary>
/// Returns merged tenant configuration for a platform caller that supplies the tenant id explicitly.
/// </summary>
public sealed record GetPlatformTenantConfigurationQuery(Guid TenantId, string Target)
    : IRequest<Result<TenantConfigurationDto>>;

public sealed class GetPlatformTenantConfigurationQueryHandler(ITenantSettingsReader settingsReader)
    : IRequestHandler<GetPlatformTenantConfigurationQuery, Result<TenantConfigurationDto>>
{
    private static readonly HashSet<string> AllowedTargets =
        new(StringComparer.OrdinalIgnoreCase) { "Web", "Api" };

    public async Task<Result<TenantConfigurationDto>> Handle(
        GetPlatformTenantConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return Result<TenantConfigurationDto>.Validation("Tenant id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Target) ||
            !AllowedTargets.Contains(request.Target.Trim()))
        {
            return Result<TenantConfigurationDto>.Validation(
                $"Invalid target '{request.Target}'. Allowed values: Web, Api.");
        }

        var normalizedTarget = request.Target.Trim();
        var snapshot = await settingsReader.GetConfigurationAsync(
            request.TenantId,
            normalizedTarget,
            cancellationToken);

        if (snapshot is null)
        {
            return Result<TenantConfigurationDto>.NotFound(
                $"Tenant '{request.TenantId}' is not active or no longer exists.");
        }

        var configuration = normalizedTarget.Equals("Web", StringComparison.OrdinalIgnoreCase)
            ? snapshot.Configuration
                .Where(pair => !pair.Key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : snapshot.Configuration;

        return Result<TenantConfigurationDto>.Success(new TenantConfigurationDto(
            snapshot.TenantId,
            snapshot.TenantName,
            normalizedTarget,
            snapshot.LoadedAtUtc,
            configuration));
    }
}
