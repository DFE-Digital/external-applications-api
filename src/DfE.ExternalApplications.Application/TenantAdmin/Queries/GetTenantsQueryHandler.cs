using DfE.ExternalApplications.Application.TenantAdmin.Commands;
using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.TenantAdmin.Queries;

public sealed record GetTenantsQuery : IRequest<Result<GetTenantsResponse>>;

public sealed record GetTenantsResponse(
    string Source,
    int TenantCount,
    IReadOnlyCollection<TenantDetailDto> Tenants);

public sealed record TenantDetailDto(Guid Id, string Name, string[] FrontendOrigins);

public sealed class GetTenantsQueryHandler(
    ITenantConfigurationProvider tenantConfigProvider)
    : IRequestHandler<GetTenantsQuery, Result<GetTenantsResponse>>
{
    public Task<Result<GetTenantsResponse>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = tenantConfigProvider.GetAllTenants();
        var details = tenants
            .Select(t => new TenantDetailDto(t.Id, t.Name, t.FrontendOrigins))
            .ToList()
            .AsReadOnly();

        var response = new GetTenantsResponse(
            tenantConfigProvider.Source,
            tenants.Count,
            details);

        return Task.FromResult(Result<GetTenantsResponse>.Success(response));
    }
}
