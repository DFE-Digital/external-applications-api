using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
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
                    throw ResultExceptionMapper.ToException(result.ErrorCode, result.Error);
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
