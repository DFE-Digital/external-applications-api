using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Exceptions;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Common.Behaviours
{
    internal class RateLimitingBehaviour<TReq, TRes>(
        IRateLimiterFactory<string> factory,
        IHttpContextAccessor httpContextAccessor,
        IPermissionCheckerService permissionCheckerService,
        [FromKeyedServices("internal")] ICustomRequestChecker internalAuthRequestChecker)
        : IPipelineBehavior<TReq, TRes>
        where TReq : IRateLimitedRequest, IRequest<TRes>
    {
        public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
        {
            var commandName = request.GetType().Name;

            var attr = request.GetType().GetCustomAttribute<RateLimitAttribute>();
            if (attr != null)
            {
                // Bypass rate limiting for admin users
                if (permissionCheckerService.IsAdmin())
                    return await next(ct);

                // Bypass rate limiting for internal auth requests
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext != null && internalAuthRequestChecker.IsValidRequest(httpContext))
                    return await next(ct);

                var user = httpContext?.User;

                var principalId = user?.FindFirstValue("appid") ?? user?.FindFirstValue("azp");

                if (string.IsNullOrEmpty(principalId))
                    principalId = user?.FindFirstValue(ClaimTypes.Email);
                
                if (string.IsNullOrEmpty(principalId))
                    principalId = httpContext?.Connection.RemoteIpAddress?.ToString();

                if (string.IsNullOrEmpty(principalId))
                            throw new InvalidOperationException("RateLimiter > Email/AppId claim missing");

                var limiter = factory.Create(attr.Max, TimeSpan.FromSeconds(attr.Seconds));
                if (!limiter.IsAllowed($"{principalId}_{commandName}"))
                    throw new RateLimitExceededException("Too many requests. Please retry later.");
            }
            return await next();
        }
    }
}
