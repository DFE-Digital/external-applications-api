using Microsoft.AspNetCore.StaticFiles;

namespace DfE.ExternalApplications.Utils.File
{
    public static class ContentTypeExtensions
    {
        // A single, shared provider with the default mappings
        private static readonly FileExtensionContentTypeProvider Provider
            = new FileExtensionContentTypeProvider();

        /// <summary>
        /// Returns the MIME content-type for the given filename (by extension),
        /// falling back to "application/octet-stream" if unknown.
        /// </summary>
        public static string GetContentType(this string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename must be provided.", nameof(fileName));

            if (!Provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }

        /// <summary>
        /// Shortcut overload for IFormFile (ASP.NET Core).
        /// </summary>
        public static string GetContentType(this Microsoft.AspNetCore.Http.IFormFile file)
            => file?.FileName.GetContentType()
               ?? throw new ArgumentNullException(nameof(file));
    }
}
