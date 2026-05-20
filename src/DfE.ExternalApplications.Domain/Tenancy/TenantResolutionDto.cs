namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Lightweight tenant identification returned from hostname resolution.
/// </summary>
public sealed record TenantResolutionDto(Guid TenantId, string TenantName, string Hostname);
