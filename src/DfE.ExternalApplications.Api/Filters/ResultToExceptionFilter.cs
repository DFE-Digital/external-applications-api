using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DfE.ExternalApplications.Api.Filters
{
    public class ResultToExceptionFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult or
                && or.Value is object val
                && val.GetType().IsGenericType
                && val.GetType().GetGenericTypeDefinition() == typeof(Result<>))
            {
                dynamic result = or.Value;

                if (!result.IsSuccess)
                {
                    Exception ex = result.ErrorCode switch
                    {
                        DomainErrorCode.NotFound => new NotFoundException(result.Error),
                        DomainErrorCode.Forbidden => new ForbiddenException(result.Error),
                        DomainErrorCode.Conflict => new ConflictException(result.Error),
                        DomainErrorCode.Validation => new ValidationException(result.Error),

                        _ => new BadRequestException(result.Error)
                    };

                    throw ex;
                }
                else
                {
                    // overwrite the result to just return the Value
                    context.Result = new ObjectResult(result.Value)
                    {
                        StatusCode = or.StatusCode ?? 200
                    };
                }
            }

            await next();
        }
    }
}
