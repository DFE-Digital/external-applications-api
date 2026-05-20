using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers;

/// <summary>
/// Requires the caller to present the <c>Platform.Host.Read</c> Entra app role.
/// </summary>
public sealed class PlatformHostRoleRequirement : IAuthorizationRequirement;
