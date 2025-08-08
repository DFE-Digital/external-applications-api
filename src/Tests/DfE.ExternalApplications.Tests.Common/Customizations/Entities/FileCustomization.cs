using AutoFixture;
using DfE.ExternalApplications.Domain.ValueObjects;
using FileId = DfE.ExternalApplications.Domain.ValueObjects.FileId;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

public class FileCustomization : ICustomization
{
    public FileId? OverrideId { get; set; }
    public ApplicationId? OverrideApplicationId { get; set; }
    public string? OverrideName { get; set; }
    public string? OverrideDescription { get; set; }
    public string? OverrideOriginalFileName { get; set; }
    public string? OverrideFileName { get; set; }
    public string? OverridePath { get; set; }
    public DateTime? OverrideUploadedOn { get; set; }
    public UserId? OverrideUploadedBy { get; set; }
    public long? OverridePathFileSize { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Domain.Entities.File>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new FileId(fixture.Create<Guid>());
            var applicationId = OverrideApplicationId ?? new ApplicationId(fixture.Create<Guid>());
            var name = OverrideName ?? fixture.Create<string>();
            var description = OverrideDescription ?? fixture.Create<string?>();
            var originalFileName = OverrideOriginalFileName ?? fixture.Create<string>();
            var fileName = OverrideFileName ?? fixture.Create<string>();
            var path = OverridePath ?? fixture.Create<string>();
            var uploadedOn = OverrideUploadedOn ?? fixture.Create<DateTime>();
            var uploadedBy = OverrideUploadedBy ?? new UserId(fixture.Create<Guid>());
            var fileSize = OverridePathFileSize ?? fixture.Create<long>();

            return new Domain.Entities.File(
                id,
                applicationId,
                name,
                description,
                originalFileName,
                fileName,
                path,
                uploadedOn,
                uploadedBy, 
                fileSize);
        }));
    }
} 