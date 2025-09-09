using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Models;
using DfE.ExternalApplications.Application.Common.Exceptions;

namespace DfE.ExternalApplications.Api.ExceptionHandlers
{
    /// <summary>
    /// Application Exception Handler
    /// </summary>
    public class ApplicationExceptionHandler : ICustomExceptionHandler
    {
        public int Priority => 20;

        public bool CanHandle(Type exceptionType)
        {
            return exceptionType.Name switch
            {
                nameof(ForbiddenException) => true,
                nameof(ConflictException) => true,
                nameof(NotFoundException) => true,
                nameof(BadRequestException) => true,
                nameof(RateLimitExceededException) => true,
                _ => false
            };
        }

        public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
        {
            var (statusCode, message) = exception.GetType().Name switch
            {
                nameof(BadRequestException) => (400, "Invalid request: " + exception.Message),
                nameof(ForbiddenException) => (401, "Unauthorized access " + exception.Message),
                nameof(ConflictException) => (409, "Conflict error " + exception.Message),
                nameof(RateLimitExceededException) => (429, "TooManyRequests: "+ exception.Message),
                nameof(NotFoundException) => (404, "Resource not found"),
                _ => (500, "An unexpected error occurred")
            };

            return new ExceptionResponse
            {
                StatusCode = statusCode,
                Message = message,
                ExceptionType = exception.GetType().Name,
                Context = context
            };
        }
    }
}
