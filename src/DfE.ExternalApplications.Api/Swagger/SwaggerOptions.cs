using Asp.Versioning.ApiExplorer;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DfE.ExternalApplications.Api.Swagger
{
    public class SwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly ITenantConfigurationProvider _tenantConfigurationProvider;

        private const string ServiceTitle = "API";
        private const string ContactName = "Support";
        private const string ContactEmail = "update_to_contact_email_here";
        private const string DepreciatedMessage = "- API version has been depreciated.";
        
        public SwaggerOptions(
            IApiVersionDescriptionProvider provider,
            ITenantConfigurationProvider tenantConfigurationProvider)
        {
            _provider = provider;
            _tenantConfigurationProvider = tenantConfigurationProvider;
        }
        
        public void Configure(string? name, SwaggerGenOptions options) => Configure(options);
        
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var desc in _provider.ApiVersionDescriptions)
            {
                var openApiInfo = new OpenApiInfo
                {
                    Title = ServiceTitle,
                    Contact = new OpenApiContact
                    {
                        Name = ContactName,
                        Email = ContactEmail 
                    },
                    Version = desc.ApiVersion.ToString()
                };
                if (desc.IsDeprecated) openApiInfo.Description += DepreciatedMessage;
                
                options.SwaggerDoc(desc.GroupName, openApiInfo);
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "User JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            });
            options.OperationFilter<AuthenticationHeaderOperationFilter>();
            options.OperationFilter<TenantHeaderOperationFilter>(_tenantConfigurationProvider);
            
            options.UseAllOfForInheritance();
            options.UseOneOfForPolymorphism();
            
            options.SelectDiscriminatorNameUsing(_ => "$type");
            options.SelectDiscriminatorValueUsing(subType => subType.Name);
        }
    }
}
