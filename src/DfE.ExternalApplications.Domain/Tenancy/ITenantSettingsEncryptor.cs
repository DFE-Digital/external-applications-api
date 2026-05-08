namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Encrypts and decrypts tenant settings JSON blobs for secret categories.
/// Implementations should use a stable, key-rotatable mechanism (e.g. ASP.NET Core Data Protection).
/// </summary>
public interface ITenantSettingsEncryptor
{
    /// <summary>
    /// Encrypts a plain-text settings JSON blob.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a previously encrypted settings JSON blob back to plain text.
    /// </summary>
    string Decrypt(string cipherText);
}
