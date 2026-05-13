namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Simple pub/sub used to broadcast "tenant configuration has been refreshed" events from the
/// <see cref="ITenantConfigurationProvider"/> implementation to any in-process consumer that
/// caches projected views of the configuration (e.g. <see cref="ITenantAuthProviderRegistry"/>).
/// <para>
/// Implementations MUST be singleton, thread-safe, and MUST swallow exceptions raised by individual
/// subscribers so one faulty consumer cannot break the refresh pipeline for the others.
/// </para>
/// </summary>
public interface ITenantConfigurationChangedNotifier
{
    /// <summary>
    /// Fired (synchronously) once the underlying configuration store has been re-read and the
    /// in-memory tenant list has been swapped in. Subscribers should rebuild their caches inline;
    /// the event is fired from the background refresh timer, not from a request thread.
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Triggers the <see cref="Changed"/> event. Called by the configuration provider at the end
    /// of a successful refresh.
    /// </summary>
    void Notify();
}
