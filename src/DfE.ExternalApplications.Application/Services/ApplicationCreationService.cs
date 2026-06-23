using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Services;

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
