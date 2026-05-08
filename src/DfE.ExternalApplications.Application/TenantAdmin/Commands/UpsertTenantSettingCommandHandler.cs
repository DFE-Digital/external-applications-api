using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.TenantAdmin.Commands;

public sealed record UpsertTenantSettingCommand(
    Guid TenantId,
    string Category,
    string Target,
    string SettingsJson,
    bool IsSecret) : IRequest<Result<UpsertTenantSettingResponse>>;

public sealed record UpsertTenantSettingResponse(
    Guid SettingId,
    bool WasCreated,
    string Category,
    string Target,
    string Message);

public sealed class UpsertTenantSettingCommandHandler(
    ITenantSettingsWriter settingsWriter)
    : IRequestHandler<UpsertTenantSettingCommand, Result<UpsertTenantSettingResponse>>
{
    public async Task<Result<UpsertTenantSettingResponse>> Handle(
        UpsertTenantSettingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await settingsWriter.UpsertSettingAsync(
                request.TenantId,
                request.Category,
                request.Target,
                request.SettingsJson,
                request.IsSecret,
                cancellationToken);

            var verb = result.WasCreated ? "created" : "updated";

            return Result<UpsertTenantSettingResponse>.Success(
                new UpsertTenantSettingResponse(
                    result.SettingId,
                    result.WasCreated,
                    result.Category,
                    result.Target,
                    $"Setting '{result.Category}' (Target={result.Target}) {verb} successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return Result<UpsertTenantSettingResponse>.NotFound(ex.Message);
        }
    }
}
