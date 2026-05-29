using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Security
{
    public class UserRole(ClaimsPrincipal user)
    {
        public bool IsAdmin => user.IsInRole("Admin");

        public bool IsCaseworker => user.IsInRole("Caseworker");
    }
}
