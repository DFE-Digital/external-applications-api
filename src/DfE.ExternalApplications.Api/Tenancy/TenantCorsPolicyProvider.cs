using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace DfE.ExternalApplications.Api.Tenancy;

public class TenantCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly DefaultCorsPolicyProvider _defaultProvider;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantCorsPolicyProvider(IOptions<CorsOptions> options, ITenantContextAccessor tenantContextAccessor)
    {
        _defaultProvider = new DefaultCorsPolicyProvider(options);
        _tenantContextAccessor = tenantContextAccessor;
    }

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (string.Equals(policyName, "Frontend", StringComparison.OrdinalIgnoreCase))
        {
            var tenant = _tenantContextAccessor.CurrentTenant;
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
