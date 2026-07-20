using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.FlexForms.Application.Common.Exceptions;

namespace GovUK.Dfe.FlexForms.Api.Filters;

/// <summary>
/// Maps application <see cref="GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response.Result{T}"/> error codes
/// to domain exceptions handled by the global exception pipeline.
/// </summary>
internal static class ResultExceptionMapper
{
    /// <summary>
    /// Converts a failed result's error code and message into the appropriate domain exception.
    /// </summary>
    public static Exception ToException(DomainErrorCode? errorCode, string? error) =>
        errorCode switch
        {
            DomainErrorCode.NotFound => new NotFoundException(error ?? "Resource not found"),
            DomainErrorCode.Forbidden when IsAuthenticationFailure(error) => new UnauthorizedException(),
            DomainErrorCode.Forbidden => new ForbiddenException(error ?? "Forbidden"),
            DomainErrorCode.Conflict => new ConflictException(error ?? "Conflict"),
            DomainErrorCode.Validation => new ValidationException(error ?? "Validation failed"),
            _ => new BadRequestException(error ?? "Bad request")
        };

    private static bool IsAuthenticationFailure(string? error) =>
        string.Equals(error, "Not authenticated", StringComparison.OrdinalIgnoreCase)
        || string.Equals(error, "No user identifier", StringComparison.OrdinalIgnoreCase);
}
