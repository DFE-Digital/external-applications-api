using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Services;

public sealed record ApplicationResponseAppendResult(
    DateTime Now,
    ApplicationResponse Response,
    ApplicationResponseAddedEvent DomainEvent);

/// <summary>
/// Domain service responsible for creating a new application response version (and the corresponding domain event payload),
/// without requiring the full <see cref="Entities.Application"/> aggregate to be loaded.
/// </summary>
public interface IApplicationResponseAppender
{
    ApplicationResponseAppendResult Create(
        ApplicationId applicationId,
        string responseBody,
        UserId createdBy,
        DateTime? now = null);
}


