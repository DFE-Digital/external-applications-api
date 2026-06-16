using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Queries;

/// <summary>
/// Builds application listing queries based on a user's resolved access scope.
/// </summary>
internal static class ApplicationListingQueryBuilder
{
    /// <summary>
    /// Lists only applications the user has explicit application permission rows for (dashboard / me/applications).
    /// Role is ignored so admins and caseworkers see only their own applications here.
    /// </summary>
    internal static IQueryable<Domain.Entities.Application> BuildMyApplicationsQuery(
        IEaRepository<Domain.Entities.Application> appRepo,
        User userWithAuthorization,
        IReadOnlyCollection<TemplateId> templateIdsFilter)
    {
        var applicationIds = userWithAuthorization.Permissions
            .Where(p => p is { ApplicationId: not null, ResourceType: ResourceType.Application })
            .Select(p => p.ApplicationId!)
            .Distinct()
            .ToList();

        IQueryable<Domain.Entities.Application> query = applicationIds.Count == 0
            ? appRepo.Query().AsNoTracking().Where(_ => false)
            : new GetApplicationsByIdsQueryObject(applicationIds)
                .Apply(appRepo.Query().AsNoTracking());

        if (templateIdsFilter.Count == 0)
            return query.Where(_ => false);

        return new GetApplicationsByTemplateIdsQueryObject(templateIdsFilter)
            .Apply(query);
    }

    internal static IQueryable<Domain.Entities.Application> BuildTemplateQuery(
        IEaRepository<Domain.Entities.Application> appRepo,
        TemplateId templateId) =>
        new GetApplicationsByTemplateIdQueryObject(templateId)
            .Apply(appRepo.Query().AsNoTracking());

    /// <summary>
    /// Applies optional listing search filters using composable query objects.
    /// </summary>
    internal static IQueryable<Domain.Entities.Application> ApplySearchFilters(
        IQueryable<Domain.Entities.Application> query,
        ApplicationListingSearchCriteria? search)
    {
        if (search is null || !search.HasAnyFilter)
            return query;

        if (!string.IsNullOrWhiteSpace(search.Reference))
            query = new GetApplicationsByReferenceSearchQueryObject(search.Reference).Apply(query);

        if (search.DateStartedFrom.HasValue || search.DateStartedTo.HasValue)
            query = new GetApplicationsByDateStartedRangeQueryObject(search.DateStartedFrom, search.DateStartedTo)
                .Apply(query);

        if (search.DateSubmittedFrom.HasValue || search.DateSubmittedTo.HasValue)
            query = new GetApplicationsByDateSubmittedRangeQueryObject(search.DateSubmittedFrom, search.DateSubmittedTo)
                .Apply(query);

        if (search.Status.HasValue)
            query = new GetApplicationsByStatusQueryObject(search.Status.Value).Apply(query);

        return query;
    }

    internal static async Task<PagedResult<ApplicationDto>> MapPagedResultAsync(
        IQueryable<Domain.Entities.Application> query,
        bool includeSchema,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        int? totalCount = null;
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            totalCount = await query.CountAsync(cancellationToken);
            var pageIndex = Math.Max(0, pageNumber.Value - 1);
            query = new PagingQuery<Domain.Entities.Application>(pageIndex, pageSize.Value).Apply(query);
        }

        var apps = await query.ToListAsync(cancellationToken);
        var count = totalCount ?? apps.Count;

        var dtoList = apps.Select(a => MapToDto(a, includeSchema)).ToList().AsReadOnly();
        var effectivePageSize = pageSize ?? count;
        var effectivePage = pageNumber ?? 1;
        var totalPages = effectivePageSize > 0
            ? (int)Math.Ceiling((double)count / effectivePageSize)
            : 1;

        return new PagedResult<ApplicationDto>
        {
            Items = dtoList,
            TotalCount = count,
            PageNumber = effectivePage,
            PageSize = effectivePageSize,
            TotalPages = totalPages
        };
    }

    internal static PagedResult<ApplicationDto> EmptyPagedResult(int? pageNumber, int? pageSize) =>
        new()
        {
            Items = Array.Empty<ApplicationDto>(),
            TotalCount = 0,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 0,
            TotalPages = 0
        };

    private static ApplicationDto MapToDto(Domain.Entities.Application a, bool includeSchema) =>
        new()
        {
            ApplicationId = a.Id!.Value,
            ApplicationReference = a.ApplicationReference,
            TemplateVersionId = a.TemplateVersionId.Value,
            DateCreated = a.CreatedOn,
            DateSubmitted = a.Status == ApplicationStatus.Submitted ? a.LastModifiedOn : null,
            Status = a.Status,
            TemplateName = a.TemplateVersion?.Template?.Name ?? string.Empty,
            TemplateSchema = includeSchema && a.TemplateVersion != null
                ? new TemplateSchemaDto
                {
                    TemplateId = a.TemplateVersion.Template?.Id?.Value ?? Guid.Empty,
                    TemplateVersionId = a.TemplateVersion.Id!.Value,
                    VersionNumber = a.TemplateVersion.VersionNumber,
                    JsonSchema = a.TemplateVersion.JsonSchema
                }
                : null
        };
}
