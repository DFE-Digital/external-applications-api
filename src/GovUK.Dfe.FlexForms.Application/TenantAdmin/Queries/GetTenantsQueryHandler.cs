using GovUK.Dfe.FlexForms.Domain.Services;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace GovUK.Dfe.FlexForms.Application.TenantAdmin.Queries;

public sealed record GetTenantsQuery : IRequest<Result<GetTenantsResponse>>;

/// <summary>
/// Returns tenant summaries visible to the caller. Tenant admins only see their own tenant.
/// </summary>
public sealed class GetTenantsQueryHandler(
    ITenantConfigurationProvider tenantConfigProvider,
    ITenantContextAccessor tenantContextAccessor,
    IPermissionCheckerService permissionChecker)
    : IRequestHandler<GetTenantsQuery, Result<GetTenantsResponse>>
{
    public Task<Result<GetTenantsResponse>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        if (!permissionChecker.IsInteractiveTenantAdmin())
        {
            return Task.FromResult(Result<GetTenantsResponse>.Forbid(
                "Only interactive Admin users (user JWT) can list tenant configuration summaries."));
        }

        var currentTenant = tenantContextAccessor.CurrentTenant;
        if (currentTenant is null)
        {
            return Task.FromResult(Result<GetTenantsResponse>.Forbid(
                "Tenant context is required to list tenant configuration."));
        }

        // Never expose the full SaaS catalogue to a tenant admin — only their resolved tenant.
        var details = new List<TenantDetailDto>
        {
            new(currentTenant.Id, currentTenant.Name, currentTenant.FrontendOrigins)
        }.AsReadOnly();

        var response = new GetTenantsResponse(
            tenantConfigProvider.Source,
            details.Count,
            details);

        return Task.FromResult(Result<GetTenantsResponse>.Success(response));
    }
}
