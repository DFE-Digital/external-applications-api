using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace GovUK.Dfe.FlexForms.Application.TenantAdmin.Commands;

public sealed record RefreshTenantConfigurationCommand : IRequest<Result<RefreshTenantConfigurationResponse>>;

public sealed class RefreshTenantConfigurationCommandHandler(
    ITenantConfigurationProvider tenantConfigProvider)
    : IRequestHandler<RefreshTenantConfigurationCommand, Result<RefreshTenantConfigurationResponse>>
{
    public async Task<Result<RefreshTenantConfigurationResponse>> Handle(
        RefreshTenantConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        await tenantConfigProvider.RefreshAsync(cancellationToken);

        var tenants = tenantConfigProvider.GetAllTenants();
        var summaries = tenants
            .Select(t => new TenantSummaryDto(t.Id, t.Name))
            .ToList()
            .AsReadOnly();

        var message = tenantConfigProvider.Source == "AppSettings"
            ? "Tenant configuration is loaded from appsettings. Cache is static."
            : "Tenant configuration refreshed successfully.";

        return Result<RefreshTenantConfigurationResponse>.Success(
            new RefreshTenantConfigurationResponse(message, tenants.Count, summaries));
    }
}
