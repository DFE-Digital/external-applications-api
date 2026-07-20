using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Events;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Services;

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


