using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Services;

public sealed class ApplicationResponseAppender : IApplicationResponseAppender
{
    public ApplicationResponseAppendResult Create(
        ApplicationId applicationId,
        string responseBody,
        UserId createdBy,
        DateTime? now = null)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new ArgumentException("Response body cannot be null or empty", nameof(responseBody));

        if (createdBy is null)
            throw new ArgumentNullException(nameof(createdBy));

        var timestamp = now ?? DateTime.UtcNow;
        var responseId = new ResponseId(Guid.NewGuid());

        var response = new ApplicationResponse(
            responseId,
            applicationId,
            responseBody,
            timestamp,
            createdBy);

        var domainEvent = new ApplicationResponseAddedEvent(
            applicationId,
            responseId,
            createdBy,
            timestamp);

        return new ApplicationResponseAppendResult(timestamp, response, domainEvent);
    }
}


