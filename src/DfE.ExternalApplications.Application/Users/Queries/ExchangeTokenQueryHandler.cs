using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public record ExchangeTokenQuery(string SubjectToken) : IRequest<Result<ExchangeTokenDto>>;

    public class ExchangeTokenQueryHandler(
        IExternalIdentityValidator externalValidator,
        IEaRepository<User> userRepo,
        IUserTokenService tokenSvc,
        IHttpContextAccessor httpCtxAcc,
        ITenantContextAccessor tenantContextAccessor,
        [FromKeyedServices("internal")] ICustomRequestChecker internalRequestChecker,
        ILogger<ExchangeTokenQueryHandler> logger)
        : IRequestHandler<ExchangeTokenQuery, Result<ExchangeTokenDto>>
    {
        public async Task<Result<ExchangeTokenDto>> Handle(ExchangeTokenQuery req, CancellationToken ct)
        {
            var validInternalAuthReq = internalRequestChecker.IsValidRequest(httpCtxAcc.HttpContext!);

            // Get tenant-specific internal auth options for multi-tenant support
            InternalServiceAuthOptions? tenantInternalAuthOptions = null;
            if (validInternalAuthReq && tenantContextAccessor.CurrentTenant != null)
            {
                tenantInternalAuthOptions = new InternalServiceAuthOptions();
                tenantContextAccessor.CurrentTenant.Settings
                    .GetSection(InternalServiceAuthOptions.SectionName)
                    .Bind(tenantInternalAuthOptions);
            }

            var externalUser = await externalValidator
                .ValidateIdTokenAsync(req.SubjectToken, false, validInternalAuthReq, tenantInternalAuthOptions, ct);

            var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;
                        
            if (email is null)
                return Result<ExchangeTokenDto>.Failure("Missing email");

            // Resolve template ID for current tenant so we can enforce template-level access (triggers auto-registration if missing).
            var requestTemplateId = ResolveRequestTemplateId(tenantContextAccessor, logger);
            if (requestTemplateId is null)
                return Result<ExchangeTokenDto>.Failure("Template could not be resolved for current tenant. Ensure ApplicationTemplates:HostMappings or DefaultTemplateKey is configured.");

            var dbUser = await (new GetUserWithAllTemplatePermissionsQueryObject(email))
                .Apply(userRepo.Query().AsNoTracking())
                .Include(u => u.Role)
                .FirstOrDefaultAsync(cancellationToken: ct);

            if (dbUser is null)
                return Result<ExchangeTokenDto>.NotFound($"User not found for email {email}");

            if (dbUser.Role is null)
                return Result<ExchangeTokenDto>.Conflict($"User {email} has no role assigned");

            // User must have access to the request's template; otherwise treat as "not found" so client auto-registration runs.
            var hasTemplateAccess = dbUser.TemplatePermissions
                .Any(tp => tp.TemplateId.Value == requestTemplateId.Value);
            if (!hasTemplateAccess)
                return Result<ExchangeTokenDto>.NotFound($"User not found for email {email}");

            var httpCtx = httpCtxAcc.HttpContext!;
            var azureAuth = await httpCtx.AuthenticateAsync("AzureEntra");
            var svcRoles = azureAuth.Succeeded
                ? azureAuth.Principal.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                : Enumerable.Empty<Claim>();

            // Create new identity with only specific claims from external user
            var identity = new ClaimsIdentity();
            var allowedClaimTypes = new[]
            {
                ClaimTypes.NameIdentifier,
                ClaimTypes.Email,
                ClaimTypes.GivenName,
                ClaimTypes.Surname,
                "organisation"
            };

            // Add allowed external claims if not already present
            foreach (var claim in externalUser.Claims)
            {
                if (allowedClaimTypes.Contains(claim.Type) &&
                    !identity.HasClaim(c => c.Type == claim.Type && c.Value == claim.Value))
                {
                    identity.AddClaim(claim);
                }
            }

            // Add the user's role if it's not already there
            if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == dbUser.Role.Name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, dbUser.Role.Name));
            }

            // Merge Azure Entra service roles, avoiding duplicates
            foreach (var svcRole in svcRoles)
            {
                var isExcludedRole =
                    (svcRole.Type == ClaimTypes.Role || svcRole.Type == "roles") &&
                    (svcRole.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                     svcRole.Value.Equals("User", StringComparison.OrdinalIgnoreCase));

                if (isExcludedRole)
                    continue;

                if (!identity.HasClaim(c => c.Type == svcRole.Type && c.Value == svcRole.Value))
                {
                    identity.AddClaim(svcRole);
                }
            }

            var mergedUser = new ClaimsPrincipal(identity);

            var internalToken = await tokenSvc.GetUserTokenModelAsync(mergedUser);

            return Result<ExchangeTokenDto>.Success(new ExchangeTokenDto
            {
                AccessToken = internalToken.AccessToken,
                TokenType = "Bearer",
                ExpiresIn = internalToken.ExpiresIn,
                RefreshToken = internalToken.RefreshToken,
                Scope = internalToken.Scope,
                IdToken = internalToken.IdToken,
                RefreshExpiresIn = internalToken.RefreshExpiresIn
            });
        }

        /// <summary>
        /// Resolves the template ID for the current request from tenant-scoped ApplicationTemplates config.
        /// Tries: DefaultTemplateKey, then tenant name as HostMappings key, then single HostMappings entry.
        /// </summary>
        private static Guid? ResolveRequestTemplateId(ITenantContextAccessor tenantContextAccessor, ILogger<ExchangeTokenQueryHandler> logger)
        {
            var tenant = tenantContextAccessor.CurrentTenant;
            if (tenant is null)
            {
                logger.LogWarning(
                    "ResolveRequestTemplateId: No current tenant. Ensure X-Tenant-ID header or Origin is set so tenant resolution can run.");
                return null;
            }

            logger.LogDebug(
                "ResolveRequestTemplateId: Tenant resolved. TenantId={TenantId}, TenantName={TenantName}",
                tenant.Id,
                tenant.Name);

            var appTemplates = tenant.Settings.GetSection("ApplicationTemplates");
            var hostMappingsSection = appTemplates.GetSection("HostMappings");
            var hostMappings = hostMappingsSection.GetChildren()
                .ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);

            if (hostMappings.Count == 0)
            {
                logger.LogWarning(
                    "ResolveRequestTemplateId: No HostMappings found for tenant {TenantName} (Id={TenantId}). " +
                    "Check ApplicationTemplates:HostMappings in appsettings for this tenant.",
                    tenant.Name,
                    tenant.Id);
                return null;
            }

            var mappingKeys = string.Join(", ", hostMappings.Keys);
            var mappingPreview = string.Join(", ", hostMappings.Select(kv => $"{kv.Key}={kv.Value ?? "(null)"}"));
            logger.LogDebug(
                "ResolveRequestTemplateId: HostMappings keys=[{Keys}], values=[{Values}]",
                mappingKeys,
                mappingPreview);

            string? templateIdString = null;
            var defaultKey = appTemplates["DefaultTemplateKey"];

            if (!string.IsNullOrEmpty(defaultKey) && hostMappings.TryGetValue(defaultKey, out var fromDefault))
            {
                templateIdString = fromDefault;
                logger.LogDebug(
                    "ResolveRequestTemplateId: Using DefaultTemplateKey. Key={DefaultTemplateKey}, TemplateId={TemplateId}",
                    defaultKey,
                    templateIdString);
            }

            if (templateIdString is null && hostMappings.TryGetValue(tenant.Name.Trim(), out var fromTenantName))
            {
                templateIdString = fromTenantName;
                logger.LogDebug(
                    "ResolveRequestTemplateId: Using tenant name as HostMappings key. TenantName={TenantName}, TemplateId={TemplateId}",
                    tenant.Name,
                    templateIdString);
            }

            if (templateIdString is null && hostMappings.Count == 1)
            {
                templateIdString = hostMappings.Values.Single();
                logger.LogDebug(
                    "ResolveRequestTemplateId: Using single HostMappings entry. TemplateId={TemplateId}",
                    templateIdString);
            }

            if (string.IsNullOrEmpty(templateIdString))
            {
                logger.LogWarning(
                    "ResolveRequestTemplateId: Could not resolve template. TenantName={TenantName}, HostMappingsKeys=[{Keys}]. " +
                    "Either set ApplicationTemplates:DefaultTemplateKey to one of the keys, or ensure tenant name matches a key (case-insensitive), or use a single HostMappings entry.",
                    tenant.Name,
                    mappingKeys);
                return null;
            }

            if (!Guid.TryParse(templateIdString, out var templateId))
            {
                logger.LogWarning(
                    "ResolveRequestTemplateId: Resolved template value is not a valid GUID. TenantName={TenantName}, RawValue={RawValue}",
                    tenant.Name,
                    templateIdString);
                return null;
            }

            logger.LogDebug(
                "ResolveRequestTemplateId: Resolved TemplateId={TemplateId} for tenant {TenantName}",
                templateId,
                tenant.Name);
            return templateId;
        }
    }
}
