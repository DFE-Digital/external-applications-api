using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;

namespace DfE.ExternalApplications.Application.Services
{
    /// <summary>
    /// Tenant-aware file storage service that automatically applies tenant-specific
    /// file storage settings (like BaseDirectory) to all operations.
    /// 
    /// This interface extends IFileStorageService and should be used throughout the
    /// application instead of the base IFileStorageService to ensure proper tenant isolation.
    /// 
    /// The implementation resolves the current tenant context and inner file storage
    /// service lazily at runtime to avoid DI lifetime issues.
    /// </summary>
    public interface ITenantAwareFileStorageService : IFileStorageService
    {
    }
}
