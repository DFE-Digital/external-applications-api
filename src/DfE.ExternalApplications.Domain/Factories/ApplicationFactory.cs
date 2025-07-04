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

    public ApplicationResponse AddResponseToApplication(
        Application application,
        string responseBody,
        UserId addedBy)
    {
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        if (string.IsNullOrWhiteSpace(responseBody))
            throw new ArgumentException("Response body cannot be null or empty", nameof(responseBody));

        if (addedBy == null)
            throw new ArgumentNullException(nameof(addedBy));

        var now = DateTime.UtcNow;
        var responseId = new ResponseId(Guid.NewGuid());
        
        var response = new ApplicationResponse(
            responseId,
            application.Id!,
            responseBody,
            now,
            addedBy);

        application.AddResponse(response);
        
        // Update application's LastModified tracking
        application.UpdateLastModified(now, addedBy);
        
        // Raise domain event
        application.AddDomainEvent(new ApplicationResponseAddedEvent(
            application.Id!,
            responseId,
            addedBy,
            now));

        return response;
    }
} 