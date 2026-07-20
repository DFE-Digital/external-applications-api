using GovUK.Dfe.FlexForms.Domain.Tenancy;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.FlexForms.Application.Common.Pipeline;

/// <summary>
/// MassTransit consume filter that resolves the tenant from the inbound message's headers
/// (set by <c>TenantAwareEventPublisher</c>) and populates <see cref="ITenantContextAccessor.CurrentTenant"/>
/// before the consumer body runs. Allows a single shared subscription to serve all tenants.
/// </summary>
public sealed class TenantContextConsumeFilter<T>(
    ITenantContextAccessor tenantContextAccessor,
    ITenantConfigurationProvider tenantConfigurationProvider,
    ILogger<TenantContextConsumeFilter<T>> logger) : IFilter<ConsumeContext<T>>
    where T : class
{
    private const string TenantIdHeader = "TenantId";

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var tenantIdHeader = context.Headers.Get<string>(TenantIdHeader);

        if (!string.IsNullOrEmpty(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            var tenant = tenantConfigurationProvider.GetTenant(tenantId);
            if (tenant is not null)
            {
                tenantContextAccessor.CurrentTenant = tenant;
                logger.LogDebug(
                    "Resolved tenant from message headers: {TenantId} ({TenantName}) for {MessageType}",
                    tenantId, tenant.Name, typeof(T).Name);
            }
            else
            {
                logger.LogWarning(
                    "Message of type {MessageType} has TenantId '{TenantId}' but no matching tenant configuration was found",
                    typeof(T).Name, tenantId);
            }
        }
        else
        {
            logger.LogWarning(
                "Message of type {MessageType} has no '{Header}' header; tenant context will not be set",
                typeof(T).Name, TenantIdHeader);
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
        => context.CreateFilterScope("tenantContext");
}
