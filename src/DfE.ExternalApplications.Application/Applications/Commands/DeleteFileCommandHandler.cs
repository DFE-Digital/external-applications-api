using MediatR;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using Microsoft.EntityFrameworkCore;
using DfE.CoreLibs.Contracts;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record DeleteFileCommand(Guid FileId) : IRequest;

public class DeleteFileCommandHandler(
    IEaRepository<Upload> uploadRepository,
    IEaRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<DeleteFileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<bool>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(JwtRegisteredClaimNames.Email);
            if (string.IsNullOrEmpty(principalId))
                return Result<bool>.Failure("No user identifier");

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
                return Result<bool>.Failure("User not found");

            // Permission check: user must have delete permission for this file
            if (!permissionCheckerService.HasPermission(ResourceType.File, request.FileId.ToString(), AccessType.Delete))
                return Result<bool>.Failure("User does not have permission to delete this file");

            var upload = uploadRepository.Query().FirstOrDefault(u => u.Id!.Value == request.FileId);
            if (upload == null)
                return Result<bool>.Failure("File not found");

            var storagePath = $"applications/{upload.ApplicationId.Value}/uploads/{upload.FileName}";
            await fileStorageService.DeleteAsync(storagePath, cancellationToken);

            uploadRepository.Remove(upload);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message);
        }
    }
} 