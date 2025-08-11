using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MediatR;

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
                    throw result.ErrorCode switch
                    {
                        DomainErrorCode.NotFound => new NotFoundException(result.Error),
                        DomainErrorCode.Forbidden => new ForbiddenException(result.Error),
                        DomainErrorCode.Conflict => new ConflictException(result.Error),
                        DomainErrorCode.Validation => new ValidationException(result.Error),
                        _ => new BadRequestException(result.Error)
                    };
                }
                else
                {

                        // For other values, return the Value as before
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
