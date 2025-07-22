using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;
using DfE.ExternalApplications.Utils.File;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record DownloadFileQuery(Guid FileId, ApplicationId ApplicationId) : IRequest<Result<DownloadFileResult>>;

public class DownloadFileQueryHandler(
    IEaRepository<File> uploadRepository,
    IEaRepository<User> userRepository,
    IFileStorageService fileStorageService,
    IEaRepository<Domain.Entities.Application> applicationRepository,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<DownloadFileQuery, Result<DownloadFileResult>>
{
    public async Task<Result<DownloadFileResult>> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<DownloadFileResult>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(principalId))
                return Result<DownloadFileResult>.Failure("No user identifier");

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
                return Result<DownloadFileResult>.Failure("User not found");

            var application = new GetApplicationByIdQueryObject(request.ApplicationId)
                .Apply(applicationRepository.Query())
                .FirstOrDefault();
            if (application == null)
                return Result<DownloadFileResult>.Failure("Application not found");

            // Permission check: user must have read permission for this file
            if (!permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read))
                return Result<DownloadFileResult>.Failure("User does not have permission to download this file");

            var upload = new GetFileByIdQueryObject(new FileId(request.FileId))
                .Apply(uploadRepository.Query())
                .FirstOrDefault();
            if (upload == null)
                return Result<DownloadFileResult>.Failure("File not found");

            var storagePath = $"{upload.Path}/{upload.FileName}";
            var fileStream = await fileStorageService.DownloadAsync(storagePath, cancellationToken);

            // Infer content type from file extension (simple approach)

            return Result<DownloadFileResult>.Success(new DownloadFileResult
            {
                FileStream = fileStream,
                FileName = upload.OriginalFileName,
                ContentType = upload.OriginalFileName.GetContentType()
            });
        }
        catch (Exception e)
        {
            return Result<DownloadFileResult>.Failure(e.Message);
        }
    }
} 