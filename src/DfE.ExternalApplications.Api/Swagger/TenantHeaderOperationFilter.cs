using System.Text.Json.Nodes;
using DfE.ExternalApplications.Api.Middleware;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.OpenApi;
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
        operation.Parameters ??= new List<IOpenApiParameter>();

        var tenants = _tenantConfigurationProvider.GetAllTenants();
        
        var tenantOptions = tenants
            .Select(t => (JsonNode)JsonValue.Create(t.Id.ToString())!)
            .ToList();

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
                Type = JsonSchemaType.String,
                Format = "uuid",
                Enum = tenantOptions.Any() ? tenantOptions : null,
                Default = tenantOptions.FirstOrDefault()
            }
        };

        operation.Parameters.Insert(0, parameter);
    }
}
