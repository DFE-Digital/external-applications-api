using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization requirement for notifications resource actions.
    /// </summary>
    public sealed class NotificationsPermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
}
