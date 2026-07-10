using Microsoft.Extensions.Configuration;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Settings;

/// <summary>
/// Reads API client settings from the application <see cref="IConfiguration"/> at startup.
/// </summary>
public sealed class ConfigurationApiClientSettingsProvider(IConfiguration configuration) : IApiClientSettingsProvider
{
    private readonly ApiClientSettings _settings = BindSettings(configuration);

    /// <inheritdoc />
    public ApiClientSettings GetSettings() => _settings;

    internal static ApiClientSettings BindSettings(IConfiguration configuration)
    {
        var settings = new ApiClientSettings();
        configuration.GetSection("ExternalApplicationsApiClient").Bind(settings);
        return settings;
    }
}
