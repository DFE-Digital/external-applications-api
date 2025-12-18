using GovUK.Dfe.CoreLibs.Security.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Application.Common.Behaviours
{
    [ExcludeFromCodeCoverage]
    public class PerformanceBehaviour<TRequest, TResponse>(
        ILogger<TRequest> logger,
        IHttpContextAccessor context)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Stopwatch _timer = new();

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _timer.Start();

            var response = await next();

            _timer.Stop();

            var elapsedMilliseconds = _timer.ElapsedMilliseconds;

            if (elapsedMilliseconds <= 1000) return response;

            var requestName = typeof(TRequest).Name;
            var identityName = context.HttpContext?.User?.Identity?.Name;

            logger.LogWarning("EAT API Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@IdentityName} {@Request}",
                requestName, elapsedMilliseconds, identityName, request);

            return response;
        }
    }

}
