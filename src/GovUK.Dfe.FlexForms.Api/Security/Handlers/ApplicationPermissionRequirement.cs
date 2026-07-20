using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers
{
    /// <summary>
    /// Authorization requirement for applications resource actions.
    /// </summary>
    public sealed class ApplicationPermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
}
