using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Domain.Factories;

public interface IFileFactory
{
    File CreateUpload(
        FileId id,
        Application application,
        string name,
        string? description,
        string originalFileName,
        string fileName,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy,
        long fileSize,
        string fileHash);
    void DeleteFile(File file);
} 