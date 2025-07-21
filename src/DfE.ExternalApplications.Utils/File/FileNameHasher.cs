using System.Security.Cryptography;
using System.Text;

namespace DfE.ExternalApplications.Utils.File
{

    public static class FileNameHasher
    {
        public static string HashFileName(string originalFileName)
        {
            var namePart = Path.GetFileNameWithoutExtension(originalFileName);
            var ext = Path.GetExtension(originalFileName);

            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(namePart);
            var hashBytes = md5.ComputeHash(inputBytes);

            var hex = BitConverter
                .ToString(hashBytes)
                .Replace("-", "")
                .ToLowerInvariant();

            return $"{hex}{ext}";
        }
    }
}
