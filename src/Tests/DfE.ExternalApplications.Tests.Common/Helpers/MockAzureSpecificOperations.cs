using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock Azure-specific operations that returns fake SAS URIs for testing
/// </summary>
public class MockAzureSpecificOperations : IAzureSpecificOperations
{
    public Task<string> GenerateSasTokenAsync(string filePath, DateTimeOffset expiresOn, string permissions, CancellationToken cancellationToken = default)
    {
        // Return a fake SAS URI for testing purposes
        // Format: mock://{filePath}?expires={timestamp}&permissions={permissions}
        var fakeSasUri = $"mock://{filePath}?expires={expiresOn:O}&permissions={permissions}";
        return Task.FromResult(fakeSasUri);
    }

    public Task<string> GenerateSasTokenAsync(string path, TimeSpan duration, string permissions = "r",
        CancellationToken token = new CancellationToken())
    {
        // Return a fake SAS URI for testing purposes
        var expiresOn = DateTimeOffset.UtcNow.Add(duration);
        var fakeSasUri = $"mock://{path}?expires={expiresOn:O}&permissions={permissions}";
        return Task.FromResult(fakeSasUri);
    }
}

