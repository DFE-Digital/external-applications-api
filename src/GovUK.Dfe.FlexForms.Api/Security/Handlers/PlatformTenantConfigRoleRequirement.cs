using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers;

/// <summary>
/// Requires the caller to present the <c>Platform.TenantConfig.Read</c> Entra app role.
/// </summary>
public sealed class PlatformTenantConfigRoleRequirement : IAuthorizationRequirement;
