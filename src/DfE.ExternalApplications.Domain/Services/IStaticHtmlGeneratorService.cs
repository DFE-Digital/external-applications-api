namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Service interface for generating static HTML snapshots of dynamically rendered pages
/// </summary>
public interface IStaticHtmlGeneratorService
{
    /// <summary>
    /// Generates a static HTML snapshot of a web page using a headless browser
    /// </summary>
    /// <param name="previewUrl">The full URL of the page to capture</param>
    /// <param name="authenticationHeaders">Authentication headers to pass to the browser for authenticated access</param>
    /// <param name="contentSelector">Optional CSS selector to extract only a specific section of the page (e.g., ".govuk-grid-row")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation that returns the static HTML content as a string</returns>
    Task<string> GenerateStaticHtmlAsync(
        string previewUrl,
        IDictionary<string, string> authenticationHeaders,
        string? contentSelector = null,
        CancellationToken cancellationToken = default);
}

