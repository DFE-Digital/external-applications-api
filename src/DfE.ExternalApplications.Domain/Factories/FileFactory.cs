using DfE.ExternalApplications.Domain.Entities;
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
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        return new File(
            id,
            applicationId,
            name,
            description,
            originalFileName,
            fileName,
            uploadedOn,
            uploadedBy
        );
    }
} 