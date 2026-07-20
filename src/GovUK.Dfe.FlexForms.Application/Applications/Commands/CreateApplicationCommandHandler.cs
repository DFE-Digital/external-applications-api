using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

using GovUK.Dfe.FlexForms.Application.Templates.Queries;

using GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;

using GovUK.Dfe.FlexForms.Domain.Interfaces;

using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;

using GovUK.Dfe.FlexForms.Domain.Services;

using GovUK.Dfe.FlexForms.Domain.ValueObjects;

using MediatR;

using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

using GovUK.Dfe.FlexForms.Application.Common.Behaviours;

using GovUK.Dfe.FlexForms.Application.Common.Attributes;

using GovUK.Dfe.FlexForms.Application.Services;

using Microsoft.EntityFrameworkCore;



namespace GovUK.Dfe.FlexForms.Application.Applications.Commands;



[RateLimit(3, 30)]

public sealed record CreateApplicationCommand(

    Guid TemplateId,

    string InitialResponseBody) : IRequest<Result<ApplicationDto>>, IRateLimitedRequest;



public sealed class CreateApplicationCommandHandler(

    IEaRepository<Domain.Entities.Application> applicationRepo,

    IEaRepository<Domain.Entities.Template> templateRepository,

    IAuthenticatedUserService authenticatedUserService,

    IApplicationCreationService applicationCreationService,

    IPermissionCheckerService permissionCheckerService,

    ITenantTemplateResolver tenantTemplateResolver,

    ISender mediator,

    IUserCacheInvalidator userCacheInvalidator,

    IUnitOfWork unitOfWork) : IRequestHandler<CreateApplicationCommand, Result<ApplicationDto>>

{

    public async Task<Result<ApplicationDto>> Handle(

        CreateApplicationCommand request,

        CancellationToken cancellationToken)

    {

        try

        {

            var currentUserResult = await authenticatedUserService.GetCurrentUserAsync(cancellationToken);

            if (!currentUserResult.IsSuccess)

            {

                return currentUserResult.ErrorCode switch

                {

                    DomainErrorCode.NotFound => Result<ApplicationDto>.NotFound(currentUserResult.Error!),

                    DomainErrorCode.Forbidden => Result<ApplicationDto>.Forbid(currentUserResult.Error!),

                    _ => Result<ApplicationDto>.Failure(currentUserResult.Error!)

                };

            }



            var dbUser = currentUserResult.Value!;

            var templateId = new TemplateId(request.TemplateId);

            if (!await tenantTemplateResolver.IsTemplateInCurrentTenantAsync(templateId, cancellationToken))

                return Result<ApplicationDto>.Forbid("Template does not belong to the current tenant");

            if (!permissionCheckerService.IsAdmin())
            {
                var template = await new GetTemplateByIdQueryObject(templateId)
                    .Apply(templateRepository.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);

                if (template is null)
                    return Result<ApplicationDto>.NotFound("Template not found");

                if (!template.IsLive)
                    return Result<ApplicationDto>.Forbid("Template is not live");
            }

            var canAccess = permissionCheckerService.HasPermission(ResourceType.Template, request.TemplateId.ToString(), AccessType.Write);



            if (!canAccess)

                return Result<ApplicationDto>.Forbid("User does not have permission to create applications for this template");



            var templateSchemaResult = await mediator.Send(

                new GetLatestTemplateSchemaByUserIdQuery(request.TemplateId, dbUser.Id!),

                cancellationToken);



            if (!templateSchemaResult.IsSuccess)

                return Result<ApplicationDto>.Failure(templateSchemaResult.Error!);



            var (application, _) = await applicationCreationService.CreateAsync(

                new TemplateVersionId(templateSchemaResult.Value!.TemplateVersionId),

                request.InitialResponseBody,

                dbUser.Id!,

                cancellationToken);



            await applicationRepo.AddAsync(application, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);



            await userCacheInvalidator.InvalidateForUserAsync(

                dbUser.Email,

                dbUser.ExternalProviderId,

                dbUser.Id!,

                cancellationToken);



            return Result<ApplicationDto>.Success(new ApplicationDto

            {

                ApplicationId = application.Id!.Value,

                ApplicationReference = application.ApplicationReference,

                TemplateVersionId = application.TemplateVersionId.Value,

                DateCreated = application.CreatedOn,

                Status = application.Status,

                TemplateSchema = templateSchemaResult.Value

            });

        }

        catch (Exception e)

        {

            return Result<ApplicationDto>.Failure(e.Message);

        }

    }

}


