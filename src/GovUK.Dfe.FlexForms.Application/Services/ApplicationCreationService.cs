using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Factories;
using GovUK.Dfe.FlexForms.Domain.Services;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Services;

/// <summary>
/// Creates application aggregates using generated references and the domain factory.
/// </summary>
public sealed class ApplicationCreationService(
    IApplicationReferenceProvider referenceProvider,
    IApplicationFactory applicationFactory) : IApplicationCreationService
{
    /// <inheritdoc />
    public async Task<(Domain.Entities.Application Application, ApplicationResponse Response)> CreateAsync(
        TemplateVersionId templateVersionId,
        string initialResponseBody,
        UserId createdBy,
        CancellationToken cancellationToken = default)
    {
        var reference = await referenceProvider.GenerateReferenceAsync(cancellationToken);
        var applicationId = new ApplicationId(Guid.NewGuid());
        var now = DateTime.UtcNow;

        return applicationFactory.CreateApplicationWithResponse(
            applicationId,
            reference,
            templateVersionId,
            initialResponseBody,
            now,
            createdBy);
    }
}
