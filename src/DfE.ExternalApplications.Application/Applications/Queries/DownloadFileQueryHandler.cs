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

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record DownloadFileQuery(Guid FileId) : IRequest<Result<DownloadFileResult>>;

public class DownloadFileQueryHandler(
    IEaRepository<Upload> uploadRepository,
    IEaRepository<User> userRepository,
    IFileStorageService fileStorageService,
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
                principalId = user.FindFirstValue(JwtRegisteredClaimNames.Email);
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

            var upload = new GetUploadByIdQueryObject(request.FileId)
                .Apply(uploadRepository.Query())
                .FirstOrDefault();
            if (upload == null)
                return Result<DownloadFileResult>.Failure("File not found");

            // Permission check: user must have read permission for this file
            if (!permissionCheckerService.HasPermission(ResourceType.File, request.FileId.ToString(), AccessType.Read))
                return Result<DownloadFileResult>.Failure("User does not have permission to download this file");

            var storagePath = $"applications/{upload.ApplicationId.Value}/uploads/{upload.FileName}";
            var fileStream = await fileStorageService.DownloadAsync(storagePath, cancellationToken);

            // Infer content type from file extension (simple approach)
            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(upload.OriginalFileName).ToLowerInvariant();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
            else if (ext == ".png") contentType = "image/png";
            else if (ext == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            else if (ext == ".xlsx") contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return Result<DownloadFileResult>.Success(new DownloadFileResult
            {
                FileStream = fileStream,
                FileName = upload.OriginalFileName,
                ContentType = contentType
            });
        }
        catch (Exception e)
        {
            return Result<DownloadFileResult>.Failure(e.Message);
        }
    }
} 