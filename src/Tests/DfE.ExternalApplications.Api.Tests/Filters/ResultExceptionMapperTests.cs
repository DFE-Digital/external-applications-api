using DfE.ExternalApplications.Api.ExceptionHandlers;
using DfE.ExternalApplications.Api.Filters;
using DfE.ExternalApplications.Application.Common.Exceptions;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Filters;

public class ResultExceptionMapperTests
{
    [Fact]
    public void ToException_ShouldReturnNotFoundException_ForNotFound()
    {
        var exception = ResultExceptionMapper.ToException(DomainErrorCode.NotFound, "Application not found");

        Assert.IsType<NotFoundException>(exception);
        Assert.Equal("Application not found", exception.Message);
    }

    [Fact]
    public void ToException_ShouldReturnForbiddenException_ForPermissionDenied()
    {
        var exception = ResultExceptionMapper.ToException(
            DomainErrorCode.Forbidden,
            "User does not have permission to read this application");

        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public void ToException_ShouldReturnUnauthorizedException_ForNotAuthenticated()
    {
        var exception = ResultExceptionMapper.ToException(DomainErrorCode.Forbidden, "Not authenticated");

        Assert.IsType<UnauthorizedException>(exception);
    }

    [Fact]
    public void ApplicationExceptionHandler_ShouldMapForbiddenExceptionTo403()
    {
        var handler = new ApplicationExceptionHandler();
        var response = handler.Handle(new ForbiddenException("User does not have permission to update this application"));

        Assert.Equal(403, response.StatusCode);
        Assert.Equal("User does not have permission to update this application", response.Message);
    }

    [Fact]
    public void ApplicationExceptionHandler_ShouldMapNotFoundExceptionTo404()
    {
        var handler = new ApplicationExceptionHandler();
        var response = handler.Handle(new NotFoundException("Application not found"));

        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public void ApplicationExceptionHandler_ShouldMapUnauthorizedExceptionTo401()
    {
        var handler = new ApplicationExceptionHandler();
        var response = handler.Handle(new UnauthorizedException());

        Assert.Equal(401, response.StatusCode);
    }
}
