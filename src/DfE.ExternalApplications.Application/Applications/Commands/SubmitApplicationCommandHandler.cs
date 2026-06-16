using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

using DfE.ExternalApplications.Application.Applications.QueryObjects;

using DfE.ExternalApplications.Domain.Interfaces;

using DfE.ExternalApplications.Domain.Interfaces.Repositories;

using DfE.ExternalApplications.Domain.Services;

using DfE.ExternalApplications.Domain.ValueObjects;

using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

using MediatR;

using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

using DfE.ExternalApplications.Application.Common.Attributes;

using DfE.ExternalApplications.Application.Common.Behaviours;

using DfE.ExternalApplications.Application.Services;

using Microsoft.EntityFrameworkCore;



namespace DfE.ExternalApplications.Application.Applications.Commands;



[RateLimit(1, 30)]

public sealed record SubmitApplicationCommand(Guid ApplicationId) : IRequest<Result<ApplicationDto>>, IRateLimitedRequest;



public sealed class SubmitApplicationCommandHandler(

    IEaRepository<Domain.Entities.Application> applicationRepo,

    IAuthenticatedUserService authenticatedUserService,

    IPermissionCheckerService permissionCheckerService,

    IUserCacheInvalidator userCacheInvalidator,

    IUnitOfWork unitOfWork) : IRequestHandler<SubmitApplicationCommand, Result<ApplicationDto>>

{

    public async Task<Result<ApplicationDto>> Handle(

        SubmitApplicationCommand request,

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



            var canAccess = permissionCheckerService.HasPermission(

                ResourceType.Application,

                request.ApplicationId.ToString(),

                AccessType.Write);



            if (!canAccess)

                return Result<ApplicationDto>.Forbid("User does not have permission to submit this application");



            var applicationId = new ApplicationId(request.ApplicationId);

            var application = await (new GetApplicationByIdQueryObject(applicationId))

                .Apply(applicationRepo.Query())

                .FirstOrDefaultAsync(cancellationToken);



            if (application is null)

                return Result<ApplicationDto>.NotFound("Application not found");



            if (application.CreatedBy != dbUser.Id)

                return Result<ApplicationDto>.Forbid("Only the user who created the application can submit it");



            var now = DateTime.UtcNow;

            application.Submit(now, dbUser.Id!, dbUser.Email, dbUser.Name);



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

                TemplateName = application.TemplateVersion?.Template?.Name ?? string.Empty,

                Status = application.Status,

                DateCreated = application.CreatedOn,

                DateSubmitted = application.LastModifiedOn,

                LatestResponse = null,

                TemplateSchema = null

            });

        }

        catch (Exception e)

        {

            return Result<ApplicationDto>.Failure(e.Message);

        }

    }

}


