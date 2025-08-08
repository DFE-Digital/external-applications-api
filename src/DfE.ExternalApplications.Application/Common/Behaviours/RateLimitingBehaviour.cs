using DfE.CoreLibs.Utilities.RateLimiting;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Common.Behaviours
{
    internal class RateLimitingBehaviour<TReq, TRes>(
        IRateLimiterFactory<string> factory,
        IHttpContextAccessor httpContextAccessor)
        : IPipelineBehavior<TReq, TRes>
        where TReq : IRateLimitedRequest, IRequest<TRes>
    {
        public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
        {
            var commandName = request.GetType().Name;

            var attr = request.GetType().GetCustomAttribute<RateLimitAttribute>();
            if (attr != null)
            {
                var user = httpContextAccessor.HttpContext?.User;

                var principalId = user?.FindFirstValue("appid") ?? user?.FindFirstValue("azp");

                if (string.IsNullOrEmpty(principalId))
                    principalId = user?.FindFirstValue(ClaimTypes.Email);

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
