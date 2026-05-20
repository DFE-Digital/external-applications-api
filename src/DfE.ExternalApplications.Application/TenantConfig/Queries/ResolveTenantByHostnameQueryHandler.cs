using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.TenantConfig.Queries;

/// <summary>
/// Resolves a tenant id from an HTTP hostname for platform callers (e.g. shared Web container).
/// </summary>
public sealed record ResolveTenantByHostnameQuery(string Hostname)
    : IRequest<Result<TenantResolutionDto>>;

public sealed class ResolveTenantByHostnameQueryHandler(ITenantHostnameResolver hostnameResolver)
    : IRequestHandler<ResolveTenantByHostnameQuery, Result<TenantResolutionDto>>
{
    public async Task<Result<TenantResolutionDto>> Handle(
        ResolveTenantByHostnameQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Hostname))
        {
            return Result<TenantResolutionDto>.Validation("Hostname is required.");
        }

        var resolution = await hostnameResolver.ResolveAsync(request.Hostname, cancellationToken);

        if (resolution is null)
        {
            return Result<TenantResolutionDto>.NotFound(
                $"No active tenant is mapped to hostname '{request.Hostname.Trim()}'.");
        }

        return Result<TenantResolutionDto>.Success(new TenantResolutionDto(
            resolution.TenantId,
            resolution.TenantName,
            resolution.Hostname));
    }
}
