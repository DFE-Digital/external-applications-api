using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Factories;

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
