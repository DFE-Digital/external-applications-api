using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Factories;

public class UploadFactory : IUploadFactory
{
    public Upload CreateUpload(
        UploadId id,
        ApplicationId applicationId,
        string name,
        string? description,
        string originalFileName,
        string fileName,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        return new Upload(
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