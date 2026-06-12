using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Loads the authenticated user entity for the current HTTP request.
/// </summary>
public sealed class AuthenticatedUserService(
    IHttpContextAccessor httpContextAccessor,
    IEaRepository<User> userRepo) : IAuthenticatedUserService
{
    /// <inheritdoc />
    public async Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User is not ClaimsPrincipal user || user.Identity?.IsAuthenticated != true)
            return Result<User>.Forbid("Not authenticated");

        var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
        if (string.IsNullOrEmpty(principalId))
            principalId = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(principalId))
            return Result<User>.Forbid("No user identifier");

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

        return dbUser is null
            ? Result<User>.NotFound("User not found")
            : Result<User>.Success(dbUser);
    }
}
