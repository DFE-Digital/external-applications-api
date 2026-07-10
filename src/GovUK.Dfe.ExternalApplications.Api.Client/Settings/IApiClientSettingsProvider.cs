namespace GovUK.Dfe.ExternalApplications.Api.Client.Settings;

/// <summary>
/// Provides <see cref="ApiClientSettings"/> for the current execution context.
/// Supports static startup configuration and per-request tenant-scoped settings.
/// </summary>
public interface IApiClientSettingsProvider
{
    /// <summary>
    /// Returns API client settings for the current context.
    /// </summary>
    ApiClientSettings GetSettings();
}
