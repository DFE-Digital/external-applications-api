using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace GovUK.Dfe.FlexForms.Api.Security;

/// <summary>
/// Invalidates named <see cref="TokenSettings"/> when tenant configuration is refreshed,
/// so exchange and TenantBearer stay on the same signing secrets without a process restart.
/// </summary>
public sealed class TenantTokenSettingsChangeTokenSource : IOptionsChangeTokenSource<TokenSettings>, IDisposable
{
    private readonly ITenantConfigurationChangedNotifier _notifier;
    private CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a change-token source wired to tenant configuration refresh notifications.
    /// </summary>
    public TenantTokenSettingsChangeTokenSource(ITenantConfigurationChangedNotifier notifier)
    {
        _notifier = notifier;
        _notifier.Changed += OnTenantConfigurationChanged;
    }

    /// <inheritdoc />
    /// <remarks>Empty name invalidates all named TokenSettings instances.</remarks>
    public string Name => string.Empty;

    /// <inheritdoc />
    public IChangeToken GetChangeToken()
    {
        lock (_lock)
        {
            return new CancellationChangeToken(_cts.Token);
        }
    }

    private void OnTenantConfigurationChanged()
    {
        CancellationTokenSource previous;
        lock (_lock)
        {
            previous = _cts;
            _cts = new CancellationTokenSource();
        }

        previous.Cancel();
        previous.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifier.Changed -= OnTenantConfigurationChanged;
        _cts.Dispose();
    }
}
