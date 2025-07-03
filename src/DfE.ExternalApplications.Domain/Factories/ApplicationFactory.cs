using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Factories;

public class ApplicationFactory : IApplicationFactory
{
    public (Application application, ApplicationResponse response) CreateApplicationWithResponse(
        ApplicationId applicationId,
        string applicationReference,
        TemplateVersionId templateVersionId,
        string initialResponseBody,
        DateTime createdOn,
        UserId createdBy)
    {
        var application = new Application(
            applicationId,
            applicationReference,
            templateVersionId,
            createdOn,
            createdBy);

        var response = new ApplicationResponse(
            new ResponseId(Guid.NewGuid()),
            applicationId,
            initialResponseBody,
            createdOn,
            createdBy);

        application.AddResponse(response);
        application.AddDomainEvent(new ApplicationCreatedEvent(
            applicationId,
            applicationReference,
            templateVersionId,
            createdBy,
            createdOn));

        return (application, response);
    }
} 