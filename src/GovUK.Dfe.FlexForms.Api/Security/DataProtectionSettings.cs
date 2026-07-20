namespace GovUK.Dfe.FlexForms.Api.Security;

/// <summary>
/// Configuration for ASP.NET Core Data Protection used to encrypt secret TenantSettings rows.
/// </summary>
public sealed class DataProtectionSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DataProtection";

    /// <summary>
    /// When true outside Local/Development, persists the key ring to Azure Blob Storage
    /// and protects it with an Azure Key Vault key.
    /// </summary>
    public bool UseAzure { get; set; }

    /// <summary>
    /// Stable application name for the Data Protection key ring.
    /// Do not change after secret TenantSettings have been encrypted.
    /// </summary>
    public string ApplicationName { get; set; } = "GovUK.Dfe.FlexForms.Api";

    /// <summary>
    /// Full blob URI for the shared key-ring XML
    /// (e.g. https://account.blob.core.windows.net/dataprotection-keys/api-keys.xml).
    /// </summary>
    public string? BlobUri { get; set; }

    /// <summary>
    /// Key Vault key identifier used to wrap the Data Protection key ring
    /// (e.g. https://vault.vault.azure.net/keys/tenant-settings-dp).
    /// </summary>
    public string? KeyVaultKeyId { get; set; }
}
