using System.Net;
using System.Security.Claims;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DfE.ExternalApplications.Application.Users.Commands;

[RateLimit(5, 30)]
public sealed record RegisterUserCommand(string SubjectToken) : IRequest<Result<UserDto>>, IRateLimitedRequest;

public sealed class RegisterUserCommandHandler(
    IEaRepository<User> userRepo,
    IExternalIdentityValidator externalValidator,
    IHttpContextAccessor httpContextAccessor,
    IUserFactory userFactory,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var externalUser = await externalValidator
            .ValidateIdTokenAsync(request.SubjectToken, cancellationToken);

        var email = externalUser.FindFirst(ClaimTypes.Email)?.Value
                    ?? throw new SecurityTokenException("ExchangeTokenQueryHandler > Missing email");

        var dbUser = await (new GetUserByEmailQueryObject(email))
            .Apply(userRepo.Query().AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (dbUser is not null)
        {
            return Result<UserDto>.Success(new UserDto
            {
                UserId = dbUser.Id!.Value,
                Name = dbUser.Name,
                Email = dbUser.Email,
                RoleId = dbUser.RoleId.Value,
                Authorization = null
            });
        }



    }
}