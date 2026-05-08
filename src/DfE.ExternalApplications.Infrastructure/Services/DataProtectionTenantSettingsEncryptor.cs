using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.DataProtection;

namespace DfE.ExternalApplications.Infrastructure.Services;

/// <summary>
/// Encrypts and decrypts tenant settings using ASP.NET Core Data Protection.
/// The purpose string "TenantSettings.v1" isolates these keys from other Data Protection consumers.
/// Key rotation is handled automatically by the Data Protection key ring.
/// </summary>
public class DataProtectionTenantSettingsEncryptor(IDataProtectionProvider dataProtectionProvider) : ITenantSettingsEncryptor
{
    private const string Purpose = "TenantSettings.v1";

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        return _protector.Protect(plainText);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        return _protector.Unprotect(cipherText);
    }
}
