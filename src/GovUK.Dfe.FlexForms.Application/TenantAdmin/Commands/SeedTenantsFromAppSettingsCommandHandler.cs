using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace GovUK.Dfe.FlexForms.Application.TenantAdmin.Commands;

public sealed record SeedTenantsFromAppSettingsCommand : IRequest<Result<SeedTenantsResponse>>;

public sealed class SeedTenantsFromAppSettingsCommandHandler(
    ITenantConfigSeeder tenantConfigSeeder)
    : IRequestHandler<SeedTenantsFromAppSettingsCommand, Result<SeedTenantsResponse>>
{
    public async Task<Result<SeedTenantsResponse>> Handle(
        SeedTenantsFromAppSettingsCommand request,
        CancellationToken cancellationToken)
    {
        await tenantConfigSeeder.SeedFromAppSettingsAsync(cancellationToken);

        return Result<SeedTenantsResponse>.Success(
            new SeedTenantsResponse("Tenant configuration seeding complete."));
    }
}
