using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers;

/// <summary>
/// Authorization requirement for any template permission.
/// </summary>
public sealed class AnyTemplatePermissionRequirement(string action) : IAuthorizationRequirement
{
    public string Action { get; } = action;
}