using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Api.Client.Security
{
    [ExcludeFromCodeCoverage]
    public class BearerTokenHandler(ITokenAcquisitionService tokenAcquisitionService, IHttpContextAccessor httpCtx) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var token = await tokenAcquisitionService.GetTokenAsync();

            // Service-To-Service token
            request.Headers.Add("X-Service-Authorization", token);

            // User Token ExtIdP
            var userToken = await httpCtx.HttpContext!
                .GetTokenAsync("id_token");
            if (!string.IsNullOrEmpty(userToken))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", userToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
