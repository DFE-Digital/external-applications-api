using DfE.ExternalApplications.Application.Services;
using System.IO;
using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock file storage service that simulates file operations without actual storage.
/// Implements both IFileStorageService (for CoreLibs compatibility) and 
/// ITenantAwareFileStorageService (for application handlers).
/// When options include AllowedExtensions, validates the file extension so integration tests
/// (e.g. UploadFileAsync_ShouldReturnBadRequest_WhenFileExtensionNotAllowed) pass.
/// </summary>
public class MockFileStorageService : ITenantAwareFileStorageService
{
    private readonly Dictionary<string, MemoryStream> _storedFiles = new();

    #region Default Methods (without options override)

    public Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken cancellationToken = default)
    {
        ValidateExtension(originalFileName, optionsOverride: null);
        ValidateFileSize(content, optionsOverride: null);
        return UploadInternalAsync(path, content, baseDirectory: null);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        return DeleteInternalAsync(path, baseDirectory: null);
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        return DownloadInternalAsync(path, baseDirectory: null);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return ExistsInternalAsync(path, baseDirectory: null);
    }

    #endregion

    #region Methods with Options Override (Multi-Tenant Support)

    public Task UploadAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
    {
        ValidateExtension(originalFileName, optionsOverride);
        ValidateFileSize(content, optionsOverride);
        return UploadInternalAsync(path, content, optionsOverride?.BaseDirectory);
    }

    public Task DeleteAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
    {
        return DeleteInternalAsync(path, optionsOverride?.BaseDirectory);
    }

    public Task<Stream> DownloadAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
    {
        return DownloadInternalAsync(path, optionsOverride?.BaseDirectory);
    }

    public Task<bool> ExistsAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
    {
        return ExistsInternalAsync(path, optionsOverride?.BaseDirectory);
    }

    #endregion

    #region Internal Implementation

    /// <summary>
    /// Default allow list when no tenant options are passed (e.g. test host still using app config).
    /// Matches typical API config so UploadFileAsync_ShouldReturnBadRequest_WhenFileExtensionNotAllowed passes.
    /// </summary>
    private static readonly string[] DefaultAllowedExtensions = { "jpg", "png", "pdf", "docx", "xlsx" };

    /// <summary>
    /// Default max file size (25MB) when no tenant options are passed.
    /// Matches test UploadFileAsync_ShouldReturnBadRequest_WhenFileSizeExceedsLimit (26MB exceeds limit).
    /// </summary>
    private const long DefaultMaxFileSizeBytes = 25 * 1024 * 1024;

    private static void ValidateExtension(string? originalFileName, LocalFileStorageOptions? optionsOverride)
    {
        var allowed = optionsOverride?.AllowedExtensions is { Length: > 0 }
            ? optionsOverride.AllowedExtensions
            : DefaultAllowedExtensions;
        var extension = Path.GetExtension(originalFileName ?? "")?.TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowed.Any(e => string.Equals(e.TrimStart('.').ToLowerInvariant(), extension, StringComparison.Ordinal)))
            throw new InvalidOperationException($"File extension is not allowed. Allowed: {string.Join(", ", allowed)}");
    }

    private static void ValidateFileSize(Stream content, LocalFileStorageOptions? optionsOverride)
    {
        if (!content.CanSeek)
            return;
        var maxBytes = optionsOverride?.MaxFileSizeBytes ?? DefaultMaxFileSizeBytes;
        if (maxBytes <= 0)
            return;
        if (content.Length > maxBytes)
            throw new InvalidOperationException($"File size exceeds the maximum allowed size of {maxBytes} bytes.");
    }

    private Task UploadInternalAsync(string path, Stream content, string? baseDirectory)
    {
        var fullPath = CombinePath(baseDirectory, path);
        
        // Store the file content in memory for testing
        var memoryStream = new MemoryStream();
        content.CopyTo(memoryStream);
        memoryStream.Position = 0;
        _storedFiles[fullPath] = memoryStream;
        
        return Task.CompletedTask;
    }

    private Task DeleteInternalAsync(string path, string? baseDirectory)
    {
        var fullPath = CombinePath(baseDirectory, path);
        _storedFiles.Remove(fullPath);
        return Task.CompletedTask;
    }

    private Task<Stream> DownloadInternalAsync(string path, string? baseDirectory)
    {
        var fullPath = CombinePath(baseDirectory, path);
        
        if (_storedFiles.TryGetValue(fullPath, out var stream))
        {
            // Return a copy of the stream so the original isn't disposed
            var copy = new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(copy);
            copy.Position = 0;
            return Task.FromResult<Stream>(copy);
        }
        
        throw new FileNotFoundException($"File not found: {fullPath}");
    }

    private Task<bool> ExistsInternalAsync(string path, string? baseDirectory)
    {
        var fullPath = CombinePath(baseDirectory, path);
        return Task.FromResult(_storedFiles.ContainsKey(fullPath));
    }

    private static string CombinePath(string? baseDirectory, string path)
    {
        if (string.IsNullOrEmpty(baseDirectory))
        {
            return path;
        }
        
        return $"{baseDirectory.TrimEnd('/', '\\')}/{path.TrimStart('/', '\\')}";
    }

    #endregion

    #region Test Helper Methods

    /// <summary>
    /// Gets all stored file paths (for testing purposes)
    /// </summary>
    public IEnumerable<string> GetStoredFilePaths() => _storedFiles.Keys;

    /// <summary>
    /// Clears all stored files (for testing purposes)
    /// </summary>
    public void Clear() => _storedFiles.Clear();

    #endregion
}

