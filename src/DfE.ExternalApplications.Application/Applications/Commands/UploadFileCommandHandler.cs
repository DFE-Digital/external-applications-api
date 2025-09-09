using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
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
using DfE.ExternalApplications.Utils.File;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;

namespace DfE.ExternalApplications.Application.Applications.Commands;

[RateLimit(1, 10)]
public sealed record UploadFileCommand(
    ApplicationId ApplicationId,
    string Name,
    string? Description,
    string OriginalFileName,
    Stream FileContent
) : IRequest<Result<UploadDto>>, IRateLimitedRequest;

public class UploadFileCommandHandler(
    IEaRepository<File> uploadRepository,
    IEaRepository<Domain.Entities.Application> applicationRepository,
    IEaRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService,
    IFileFactory fileFactory,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService)
    : IRequestHandler<UploadFileCommand, Result<UploadDto>>
{
    public async Task<Result<UploadDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<UploadDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(principalId))
                return Result<UploadDto>.Forbid("No user identifier");

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
                return Result<UploadDto>.NotFound("User not found");

            // Fetch application and its reference
            var application = new GetApplicationByIdQueryObject(request.ApplicationId)
                .Apply(applicationRepository.Query())
                .FirstOrDefault();
            if (application == null)
                return Result<UploadDto>.NotFound("Application not found");

            // Permission check: user must have write permission for this application (File resource)
            if (!permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Write))
                return Result<UploadDto>.Forbid("User does not have permission to upload files for this application");

            // Generate hashed file name
            var hashedFileName = FileNameHasher.HashFileName(request.OriginalFileName);
            var storagePath = $"{application.ApplicationReference}/{hashedFileName}";

            var existingFile = new GetFileByFileNameApplicationIdQueryObject(hashedFileName, application.Id)
                .Apply(uploadRepository.Query())
                .FirstOrDefault();
            if (existingFile != null)
                return Result<UploadDto>.Conflict("The file already exist");

            // Upload file to the storage
            await fileStorageService.UploadAsync(storagePath, request.FileContent, request.OriginalFileName, cancellationToken);

            var fileSize = request.FileContent.Length;

            // Create File entity using factory
            var upload = fileFactory.CreateUpload(
                new FileId(Guid.NewGuid()),
                request.ApplicationId,
                request.Name,
                request.Description,
                request.OriginalFileName,
                hashedFileName,
                application.ApplicationReference,
                DateTime.UtcNow,
                dbUser.Id!,
                fileSize
            );

            await uploadRepository.AddAsync(upload, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            var dto = new UploadDto
            {
                Id = upload.Id!.Value,
                ApplicationId = upload.ApplicationId.Value,
                UploadedBy = upload.UploadedBy.Value,
                Name = upload.Name,
                Description = upload.Description,
                OriginalFileName = upload.OriginalFileName,
                FileName = upload.FileName,
                UploadedOn = upload.UploadedOn,
                FileSize = fileSize
            };

            return Result<UploadDto>.Success(dto);
        }
        catch (Exception e)
        {
            return Result<UploadDto>.Failure(e.Message);
        }
    }
} 