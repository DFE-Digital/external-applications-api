using AutoFixture;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

public class ApplicationResponseCustomization : ICustomization
{
    public ResponseId? OverrideId { get; set; }
    public ApplicationId? OverrideAppId { get; set; }
    public string? OverrideResponseBody { get; set; }
    public DateTime? OverrideCreatedOn { get; set; }
    public UserId? OverrideCreatedBy { get; set; }
    public DateTime? OverrideLastModifiedOn { get; set; }
    public UserId? OverrideLastModifiedBy { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<ApplicationResponse>(composer => composer.FromFactory(() =>
        {
            var id = OverrideId ?? new ResponseId(fixture.Create<Guid>());
            var appId = OverrideAppId ?? new ApplicationId(fixture.Create<Guid>());
            var responseBody = (OverrideResponseBody ?? fixture.Create<string>()).Trim();
            var createdOn = OverrideCreatedOn ?? fixture.Create<DateTime>();
            var createdBy = OverrideCreatedBy ?? new UserId(fixture.Create<Guid>());
            var lastModifiedOn = OverrideLastModifiedOn;
            var lastModifiedBy = OverrideLastModifiedBy;

            return new ApplicationResponse(
                id,
                appId,
                responseBody,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy);
        }));
    }
}