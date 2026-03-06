using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DfE.ExternalApplications.Api.Swagger
{
    public class AuthenticationHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();

            var userScheme = new OpenApiSecuritySchemeReference("Bearer");
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [userScheme] = new List<string>()
            });
        }
    }
}
