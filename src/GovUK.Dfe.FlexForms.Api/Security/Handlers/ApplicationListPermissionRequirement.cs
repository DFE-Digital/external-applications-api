using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers;

/// <summary>
/// Authorization requirement for reading the list of applications a user can access.
/// </summary>
public sealed class ApplicationListPermissionRequirement(string action) : IAuthorizationRequirement
{
    public string Action { get; } = action;
}
