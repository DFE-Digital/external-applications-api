using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.Queries;
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
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed class CreateApplicationCommandHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IApplicationReferenceProvider referenceProvider,
    ITemplatePermissionService templatePermissionService,
    IApplicationFactory applicationFactory,
    ISender mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateApplicationCommand, Result<ApplicationDto>>
{
    public async Task<Result<ApplicationDto>> Handle(
        CreateApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<ApplicationDto>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<ApplicationDto>.Failure("No user identifier");

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
                return Result<ApplicationDto>.Failure("User not found");

            var canAccess = await templatePermissionService.CanUserCreateApplicationForTemplate(
                dbUser.Id!,
                request.TemplateId,
                cancellationToken);

            if (!canAccess)
                return Result<ApplicationDto>.Failure("User does not have permission to create applications for this template");

            var templateSchemaResult = await mediator.Send(
                new GetLatestTemplateSchemaByUserIdQuery(request.TemplateId, dbUser.Id!),
                cancellationToken);

            if (!templateSchemaResult.IsSuccess)
                return Result<ApplicationDto>.Failure(templateSchemaResult.Error!);

            var reference = await referenceProvider.GenerateReferenceAsync(cancellationToken);
            var applicationId = new ApplicationId(Guid.NewGuid());
            var now = DateTime.UtcNow;

            var (application, _) = applicationFactory.CreateApplicationWithResponse(
                applicationId,
                reference,
                new TemplateVersionId(templateSchemaResult.Value!.TemplateVersionId),
                request.InitialResponseBody,
                now,
                dbUser.Id!);

            await applicationRepo.AddAsync(application, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<ApplicationDto>.Success(new ApplicationDto
            {
                ApplicationId = application.Id!.Value,
                ApplicationReference = application.ApplicationReference,
                TemplateVersionId = application.TemplateVersionId.Value,
                DateCreated = application.CreatedOn,
                Status = application.Status
            });
        }
        catch (Exception e)
        {
            return Result<ApplicationDto>.Failure(e.Message);
        }
    }
} 