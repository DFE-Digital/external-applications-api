using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace DfE.ExternalApplications.Api.Tenancy;

public class TenantCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly DefaultCorsPolicyProvider _defaultProvider;

    public TenantCorsPolicyProvider(IOptions<CorsOptions> options)
    {
        _defaultProvider = new DefaultCorsPolicyProvider(options);
    }

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (string.Equals(policyName, "Frontend", StringComparison.OrdinalIgnoreCase))
        {
            // Resolve the scoped ITenantContextAccessor from the request's service provider
            var tenantContextAccessor = context.RequestServices.GetService<ITenantContextAccessor>();
            var tenant = tenantContextAccessor?.CurrentTenant;
            
            if (tenant?.FrontendOrigins is { Length: > 0 })
            {
                var builder = new CorsPolicyBuilder()
                    .WithOrigins(tenant.FrontendOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();

                return Task.FromResult<CorsPolicy?>(builder.Build());
            }
        }

        return _defaultProvider.GetPolicyAsync(context, policyName);
    }
}
