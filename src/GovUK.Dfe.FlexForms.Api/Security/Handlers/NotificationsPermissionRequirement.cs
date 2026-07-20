using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers
{
    /// <summary>
    /// Authorization requirement for notifications resource actions.
    /// </summary>
    public sealed class NotificationsPermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
}
