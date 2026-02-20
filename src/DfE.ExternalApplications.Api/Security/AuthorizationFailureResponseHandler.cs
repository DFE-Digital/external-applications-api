using DfE.ExternalApplications.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace DfE.ExternalApplications.Api.Security;

/// <summary>
/// When authorization fails (401/403), throws domain exceptions so the global exception handler
/// (<see cref="GovUK.Dfe.CoreLibs.Http.Extensions.UseGlobalExceptionHandler"/>) and
/// <see cref="ExceptionHandlers.ApplicationExceptionHandler"/> produce the same ExceptionResponse
/// as for other errors. This keeps auth failures aligned with DDD and the existing AddCustomExceptionHandler pipeline.
/// </summary>
public sealed class AuthorizationFailureResponseHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            throw new UnauthorizedException();
        }

        if (authorizeResult.Forbidden)
        {
            throw new AuthorizationForbiddenException();
        }

        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
