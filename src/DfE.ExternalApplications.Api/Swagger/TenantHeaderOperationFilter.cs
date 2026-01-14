using DfE.ExternalApplications.Api.Middleware;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DfE.ExternalApplications.Api.Swagger;

/// <summary>
/// Adds the X-Tenant-ID header to all Swagger operations with a dropdown of available tenants.
/// </summary>
public class TenantHeaderOperationFilter : IOperationFilter
{
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider;

    public TenantHeaderOperationFilter(ITenantConfigurationProvider tenantConfigurationProvider)
    {
        _tenantConfigurationProvider = tenantConfigurationProvider;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        var tenants = _tenantConfigurationProvider.GetAllTenants();
        
        // Create enum values for the dropdown
        var tenantOptions = tenants
            .Select(t => new OpenApiString(t.Id.ToString()))
            .Cast<IOpenApiAny>()
            .ToList();

        // Build description with tenant names for reference
        var tenantDescriptions = string.Join("\n", 
            tenants.Select(t => $"- `{t.Id}` = {t.Name}"));

        var parameter = new OpenApiParameter
        {
            Name = TenantResolutionMiddleware.TenantIdHeader,
            In = ParameterLocation.Header,
            Required = true,
            Description = $"Tenant identifier (GUID). Available tenants:\n{tenantDescriptions}",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Enum = tenantOptions.Any() ? tenantOptions : null,
                Default = tenantOptions.FirstOrDefault()
            }
        };

        operation.Parameters.Insert(0, parameter);
    }
}

