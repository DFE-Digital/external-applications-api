using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock file storage service that simulates file operations without actual storage
/// </summary>
public class MockFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, MemoryStream> _storedFiles = new();

    public Task UploadAsync(string path, Stream content, string originalFileName, CancellationToken cancellationToken = default)
    {
        // Store the file content in memory for testing
        var memoryStream = new MemoryStream();
        content.CopyTo(memoryStream);
        memoryStream.Position = 0;
        _storedFiles[path] = memoryStream;
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        _storedFiles.Remove(path);
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_storedFiles.TryGetValue(path, out var stream))
        {
            // Return a copy of the stream so the original isn't disposed
            var copy = new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(copy);
            copy.Position = 0;
            return Task.FromResult<Stream>(copy);
        }
        
        throw new FileNotFoundException($"File not found: {path}");
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storedFiles.ContainsKey(path));
    }
}

