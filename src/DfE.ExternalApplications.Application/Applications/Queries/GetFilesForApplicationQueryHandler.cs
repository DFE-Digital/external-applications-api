using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetFilesForApplicationQuery(ApplicationId ApplicationId) : IRequest<Result<IReadOnlyCollection<UploadDto>>>;

public class GetFilesForApplicationQueryHandler(
    IEaRepository<File> uploadRepository,
    IEaRepository<Domain.Entities.Application> applicationRepository,
    IEaRepository<User> userRepository,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService)
    : IRequestHandler<GetFilesForApplicationQuery, Result<IReadOnlyCollection<UploadDto>>>
{
    public async Task<Result<IReadOnlyCollection<UploadDto>>> Handle(GetFilesForApplicationQuery request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
            return Result<IReadOnlyCollection<UploadDto>>.Failure("Not authenticated");

        var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
        if (string.IsNullOrEmpty(principalId))
            principalId = user.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(principalId))
            return Result<IReadOnlyCollection<UploadDto>>.Failure("No user identifier");

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
            return Result<IReadOnlyCollection<UploadDto>>.Failure("User not found");

        // Fetch application and its reference
        var application = new GetApplicationByIdQueryObject(request.ApplicationId)
            .Apply(applicationRepository.Query())
            .FirstOrDefault();
        if (application == null)
            return Result<IReadOnlyCollection<UploadDto>>.Failure("Application not found");

        // Permission check: user must have read permission for this application (File resource)
        if (!permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read))
            return Result<IReadOnlyCollection<UploadDto>>.Failure("User does not have permission to list files for this application");

        var uploads = (await new GetFilesByApplicationIdQueryObject(request.ApplicationId)
            .Apply(uploadRepository.Query())
            .Select(u => new UploadDto
            {
                Id = u.Id!.Value,
                ApplicationId = u.ApplicationId.Value,
                UploadedBy = u.UploadedBy.Value,
                Name = u.Name,
                Description = u.Description,
                OriginalFileName = u.OriginalFileName,
                FileName = u.FileName,
                UploadedOn = u.UploadedOn
            })
            .ToListAsync(cancellationToken)).AsReadOnly();

        return Result<IReadOnlyCollection<UploadDto>>.Success(uploads);
    }
} 