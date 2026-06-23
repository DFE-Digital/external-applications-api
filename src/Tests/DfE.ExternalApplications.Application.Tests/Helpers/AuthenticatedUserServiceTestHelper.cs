using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Entities;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Helpers;

internal static class AuthenticatedUserServiceTestHelper
{
    public static IAuthenticatedUserService MockReturningUser(User user)
    {
        var service = Substitute.For<IAuthenticatedUserService>();
        service.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Result<User>.Success(user));
        return service;
    }

    public static IAuthenticatedUserService MockReturning(Result<User> result)
    {
        var service = Substitute.For<IAuthenticatedUserService>();
        service.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(result);
        return service;
    }
}
