using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization.Handlers
{
    /// <summary>
    /// Authorization requirement for user resource actions.
    /// </summary>
    public sealed class UserPermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
}
