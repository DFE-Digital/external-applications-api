using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    public sealed class TemplatePermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
}
