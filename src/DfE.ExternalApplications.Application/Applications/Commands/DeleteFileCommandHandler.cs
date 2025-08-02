using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.ExternalApplications.Domain.Factories;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record DeleteFileCommand(Guid FileId, Guid ApplicationId) : IRequest<Result<bool>>;

public class DeleteFileCommandHandler(
    IEaRepository<File> fileRepository,
    IEaRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IFileStorageService fileStorageService,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor,
    IFileFactory fileFactory)
    : IRequestHandler<DeleteFileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<bool>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(principalId))
                return Result<bool>.Forbid("No user identifier");

            User? dbUser;
            if (principalId.Contains('@'))
            {
                dbUser = await (new GetUserByEmailQueryObject(principalId))
                    .Apply(userRepository.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                dbUser = await (new GetUserByExternalProviderIdQueryObject(principalId))
                    .Apply(userRepository.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (dbUser is null)
                return Result<bool>.NotFound("User not found");

            // Get the application to verify it exists
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<bool>.NotFound("Application not found");

            // Check if user is the application owner or admin
            var isOwner = permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString());
            var isAdmin = permissionCheckerService.IsAdmin();

            if (!isOwner && !isAdmin)
                return Result<bool>.Failure("Only the application owner or admin can remove files");

            // Permission check: user must have delete permission for this file
            if (!permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, request.ApplicationId.ToString(),
                    AccessType.Delete))
                return Result<bool>.Forbid("User does not have permission to delete this file");

            var upload = await new GetFileByIdQueryObject(new FileId(request.FileId))
                .Apply(fileRepository.Query())
                .FirstOrDefaultAsync(cancellationToken);
            if (upload == null)
                return Result<bool>.NotFound("File not found");

            var storagePath = $"{application.ApplicationReference}/{upload.FileName}";
            await fileStorageService.DeleteAsync(storagePath, cancellationToken);

            fileFactory.DeleteFile(upload);

            await fileRepository.RemoveAsync(upload, cancellationToken);
            
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message);
        }
    }
} 