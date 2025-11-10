using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Domain.Factories;

public class FileFactory : IFileFactory
{
    public File CreateUpload(
        FileId id,
        ApplicationId applicationId,
        string name,
        string? description,
        string originalFileName,
        string fileName,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy,
        long fileSize,
        string fileHash)
    {
        var file = new File(
            id,
            applicationId,
            name,
            description,
            originalFileName,
            fileName,
            path,
            uploadedOn,
            uploadedBy,
            fileSize
        );

        file.AddDomainEvent(new FileUploadedDomainEvent(file, fileHash,uploadedOn));

        return file;
    }

    public void DeleteFile(File file)
    {
        file.Delete();

        var when = DateTime.UtcNow;

        file.AddDomainEvent(new FileDeletedEvent(file.Id!, when));
    }
} 