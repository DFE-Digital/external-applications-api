using Microsoft.AspNetCore.Authentication;

namespace GovUK.Dfe.FlexForms.Api.Security.Schemes;

/// <summary>
/// Options for <see cref="ApiKeyAuthenticationHandler"/>. The header name defaults to
/// <c>X-Api-Key</c>; override it per-deployment if a downstream gateway rewrites the header
/// before it reaches the API.
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>The HTTP header carrying the shared API key.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";
}
