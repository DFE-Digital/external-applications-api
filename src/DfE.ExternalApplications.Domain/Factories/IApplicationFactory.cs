using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Factories;

public interface IApplicationFactory
{
    (Application application, ApplicationResponse response) CreateApplicationWithResponse(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        string initialResponseBody,
        DateTime createdOn,
        UserId createdBy);

    ApplicationResponse AddResponseToApplication(
        Application application,
        string responseBody,
        UserId addedBy);
} 