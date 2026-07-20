// Temporary shim until GovUK.Dfe.CoreLibs.Contracts package includes HostConfigurationDto.
// Remove this file after upgrading the Contracts package.
namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <inheritdoc cref="HostConfigurationDto"/>
public sealed record HostConfigurationDto(
    string Target,
    DateTime LoadedAtUtc,
    IReadOnlyDictionary<string, string?> Configuration);
