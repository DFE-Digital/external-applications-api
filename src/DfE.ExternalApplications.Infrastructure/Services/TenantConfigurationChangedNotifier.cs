using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Infrastructure.Services;

/// <summary>
/// In-process pub/sub used to broadcast tenant-configuration refresh events from
/// <see cref="DatabaseTenantConfigurationProvider"/> to any subscriber that needs to rebuild
/// derived caches (e.g. <see cref="ITenantAuthProviderRegistry"/>). Thread-safe; subscriber
/// exceptions are caught so one faulty consumer cannot break the refresh pipeline.
/// </summary>
public sealed class TenantConfigurationChangedNotifier(
    ILogger<TenantConfigurationChangedNotifier> logger) : ITenantConfigurationChangedNotifier
{
    private readonly object _gate = new();

    public event Action? Changed;

    public void Notify()
    {
        Action? handlers;
        lock (_gate)
        {
            handlers = Changed;
        }

        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Action>())
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Tenant configuration changed subscriber threw. Continuing to notify the remaining subscribers.");
            }
        }
    }
}
