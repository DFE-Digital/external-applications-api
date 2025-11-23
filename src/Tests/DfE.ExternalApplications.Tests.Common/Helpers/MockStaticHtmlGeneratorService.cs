using DfE.ExternalApplications.Domain.Services;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock implementation of IStaticHtmlGeneratorService for testing purposes
/// Returns a simple HTML response without invoking actual Playwright/browser automation
/// </summary>
public class MockStaticHtmlGeneratorService : IStaticHtmlGeneratorService
{
    public Task<string> GenerateStaticHtmlAsync(
        string previewUrl,
        IDictionary<string, string> authenticationHeaders,
        string? contentSelector = null,
        CancellationToken cancellationToken = default)
    {
        // Return a mock HTML response that simulates what Playwright would generate
        var html = $@"<!DOCTYPE html>
<html lang=""en"" class=""govuk-template"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Application Preview - Mock</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; }}
        .govuk-grid-row {{ margin: 20px 0; }}
        .preview-content {{ background: #f3f2f1; padding: 15px; border-radius: 4px; }}
    </style>
</head>
<body class=""govuk-template__body"">
    <div class=""govuk-width-container"">
        <div class=""govuk-grid-row"">
            <div class=""preview-content"">
                <h1>Mock Application Preview</h1>
                <p>URL: {previewUrl}</p>
                <p>Content Selector: {contentSelector ?? "Full Page"}</p>
                <p>Authentication Headers: {authenticationHeaders.Count} header(s)</p>
                <div class=""application-details"">
                    <h2>Application Information</h2>
                    <p>This is a mock static HTML generated for testing purposes.</p>
                    <p>In production, this would be the actual rendered application page.</p>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

        return Task.FromResult(html);
    }
}

