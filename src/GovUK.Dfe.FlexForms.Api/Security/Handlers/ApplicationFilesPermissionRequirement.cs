using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers
{
    /// <summary>
    /// Authorization requirement for file resource actions.
    /// </summary>
    public sealed class ApplicationFilesPermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
} 
