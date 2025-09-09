using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.ExternalApplications.Application.Common.Behaviours;

namespace DfE.ExternalApplications.Application.Templates.Commands;

[RateLimit(1, 30)]
public record CreateTemplateVersionCommand(
    Guid TemplateId,
    string VersionNumber,
    string JsonSchema) : IRequest<Result<TemplateSchemaDto>>, IRateLimitedRequest;

public sealed class CreateTemplateVersionCommandHandler(
    IEaRepository<Template> templateRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionChecker,
    ITemplateFactory templateFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTemplateVersionCommand, Result<TemplateSchemaDto>>
{
    public async Task<Result<TemplateSchemaDto>> Handle(
        CreateTemplateVersionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<TemplateSchemaDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<TemplateSchemaDto>.Forbid("No user identifier");

            User? dbUser;
            if (principalId.Contains('@'))
            {
                dbUser = await (new GetUserByEmailQueryObject(principalId))
                    .Apply(userRepo.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                dbUser = await (new GetUserByExternalProviderIdQueryObject(principalId))
                    .Apply(userRepo.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (dbUser is null)
                return Result<TemplateSchemaDto>.NotFound("User not found");

            var base64EncodedBytes = System.Convert.FromBase64String(request.JsonSchema);
            var decodedJsonSchema = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

            var template = await new GetTemplateByIdQueryObject(new TemplateId(request.TemplateId))
                .Apply(templateRepo.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (template is null)
            {
                return Result<TemplateSchemaDto>.NotFound("Template not found");
            }

            if (!permissionChecker.HasTemplatePermission(template.Id?.Value.ToString()!, AccessType.Write))
            {
                return Result<TemplateSchemaDto>.Forbid("Access denied");
            }

            if (template.TemplateVersions?.Any(v => v.VersionNumber == request.VersionNumber) == true)
            {
                return Result<TemplateSchemaDto>.Validation($"Version {request.VersionNumber} already exists");
            }

            var newVersion = templateFactory.AddVersionToTemplate(
                template,
                request.VersionNumber,
                decodedJsonSchema,
                dbUser.Id!);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<TemplateSchemaDto>.Success(new TemplateSchemaDto
            {
                TemplateId = template.Id!.Value,
                TemplateVersionId = newVersion.Id!.Value,
                VersionNumber = newVersion.VersionNumber,
                JsonSchema = newVersion.JsonSchema
            });
        }
        catch (FormatException)
        {
            return Result<TemplateSchemaDto>.Failure("Invalid Base64 format for JsonSchema");
        }
        catch (ArgumentException ex)
        {
            return Result<TemplateSchemaDto>.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<TemplateSchemaDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<TemplateSchemaDto>.Failure(ex.ToString());
        }
    }
}