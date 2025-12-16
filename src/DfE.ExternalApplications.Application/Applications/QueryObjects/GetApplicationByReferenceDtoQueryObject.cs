using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

/// <summary>
/// Optimized read/query for "application by reference" that:
/// - Fetches ONLY the latest response (not the full response history)
/// - Only fetches JsonSchema when includeSchema=true
/// - Projects directly to ApplicationDto to avoid loading entire entity graphs
/// </summary>
public sealed class GetApplicationByReferenceDtoQueryObject(string applicationReference, bool includeSchema = true)
{
    public IQueryable<ApplicationDto> Apply(IQueryable<Domain.Entities.Application> query)
    {
        // Note: we intentionally do NOT use Include(...) here.
        // Projection ensures EF selects only the referenced columns.
        return query
            .AsNoTracking()
            .Where(a => a.ApplicationReference == applicationReference)
            .Select(a => new ApplicationDto
            {
                ApplicationId = a.Id!.Value,
                ApplicationReference = a.ApplicationReference,
                TemplateVersionId = a.TemplateVersionId.Value,
                TemplateName = a.TemplateVersion!.Template!.Name,
                Status = a.Status,
                DateCreated = a.CreatedOn,
                DateSubmitted = a.Status == GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums.ApplicationStatus.Submitted
                    ? a.LastModifiedOn
                    : null,
                LatestResponse = a.Responses
                    .OrderByDescending(r => r.CreatedOn)
                    .Select(r => new ApplicationResponseDetailsDto
                    {
                        ResponseId = r.Id!.Value,
                        ResponseBody = r.ResponseBody,
                        CreatedOn = r.CreatedOn,
                        CreatedBy = r.CreatedBy.Value,
                        LastModifiedOn = r.LastModifiedOn,
                        LastModifiedBy = r.LastModifiedBy != null ? r.LastModifiedBy.Value : null
                    })
                    .FirstOrDefault(),
                TemplateSchema = includeSchema
                    ? new TemplateSchemaDto
                    {
                        TemplateId = a.TemplateVersion!.Template!.Id!.Value,
                        TemplateVersionId = a.TemplateVersion!.Id!.Value,
                        VersionNumber = a.TemplateVersion!.VersionNumber,
                        JsonSchema = a.TemplateVersion!.JsonSchema
                    }
                    : null,
                CreatedBy = a.CreatedByUser == null
                    ? null
                    : new UserDto
                    {
                        UserId = a.CreatedByUser.Id!.Value,
                        Name = a.CreatedByUser.Name,
                        Email = a.CreatedByUser.Email
                    }
            });
    }
}


