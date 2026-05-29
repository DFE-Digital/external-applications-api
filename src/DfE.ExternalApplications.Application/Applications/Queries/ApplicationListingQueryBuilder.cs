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
    internal static IQueryable<Domain.Entities.Application> BuildQuery(
        IEaRepository<Domain.Entities.Application> appRepo,
        User userWithAuthorization,
        Guid? templateIdFilter)
    {
        var scope = ApplicationAccessResolver.Resolve(userWithAuthorization);

        IQueryable<Domain.Entities.Application> query = scope.Mode switch
        {
            ApplicationAccessResolver.AccessMode.AllApplicationsInTenant =>
                new GetAllApplicationsQueryObject().Apply(appRepo.Query().AsNoTracking()),

            ApplicationAccessResolver.AccessMode.TemplateScoped =>
                new GetApplicationsByTemplateIdsQueryObject(scope.TemplateIds)
                    .Apply(appRepo.Query().AsNoTracking()),

            _ when scope.ApplicationIds.Count == 0 =>
                appRepo.Query().AsNoTracking().Where(_ => false),

            _ =>
                new GetApplicationsByIdsQueryObject(scope.ApplicationIds)
                    .Apply(appRepo.Query().AsNoTracking())
        };

        if (templateIdFilter.HasValue)
            query = new GetApplicationsByTemplateIdQueryObject(new TemplateId(templateIdFilter.Value))
                .Apply(query);

        return query;
    }

    internal static IQueryable<Domain.Entities.Application> BuildTemplateQuery(
        IEaRepository<Domain.Entities.Application> appRepo,
        TemplateId templateId) =>
        new GetApplicationsByTemplateIdQueryObject(templateId)
            .Apply(appRepo.Query().AsNoTracking());

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
