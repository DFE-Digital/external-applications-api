using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers;

/// <summary>
/// Requires an interactive tenant Admin user (user JWT), not a machine/service identity.
/// </summary>
public sealed class TenantAdminUserRequirement : IAuthorizationRequirement
{
}
