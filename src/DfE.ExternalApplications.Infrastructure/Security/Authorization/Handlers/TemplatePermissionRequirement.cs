using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization.Handlers
{
    public sealed class TemplatePermissionRequirement(string action) : IAuthorizationRequirement
    {
        public string Action { get; } = action;
    }
}
