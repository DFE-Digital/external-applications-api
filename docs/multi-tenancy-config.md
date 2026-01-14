# Multi-tenant configuration

The API keeps domain logic tenant-agnostic, but infrastructure concerns (CORS, connection strings, SignalR, storage, etc.) are now selected per tenant. Each tenant is declared inside the primary `appsettings.<Environment>.json` files so the platform can load everything at startup without extra configuration files.

## Configuration shape
```jsonc
{
  "Tenants": {
    "11111111-1111-1111-1111-111111111111": {
      "Id": "11111111-1111-1111-1111-111111111111",
      "Name": "Alpha",
      "Logging": {
        "LogLevel": { "Default": "Warning" }
      },
      "ConnectionStrings": {
        "DefaultConnection": "Server=...;Database=AlphaExternalApps;...",
        "AzureSignalR": "Endpoint=...;AccessKey=..."
      },
      "AzureAd": { "ClientId": "..." },
      "Frontend": { "Origin": "https://alpha.example.com" },
      "FrontendSettings": { "BaseUrl": "https://alpha.example.com" },
      "InternalServiceAuth": {
        "Services": [
          { "Email": "svc-alpha@service.com", "ApiKey": "secret" }
        ],
        "TokenLifetimeMinutes": 60
      },
      "FileStorage": { "Azure": { "ShareName": "alpha-storage" } },
      "Email": { "ServiceSupportEmailAddress": "support@alpha.example.com" },
      "Features": { "PerformanceLoggingEnabled": true }
    },
    "22222222-2222-2222-2222-222222222222": { "Id": "...", "Name": "Beta", "Logging": { ... }, "ConnectionStrings": { ... }, "AzureAd": { ... } }
  }
}
```

- The **dictionary key** and `Id` property must both be the tenant GUID. The `Name` is required for observability and validation.
- Wrap the entire set of environment-specific options inside each tenant block. You can copy/paste the existing `appsettings.<Environment>.json` content beneath each tenant, then customise per tenant as needed.
- Host-level settings that are not tenant-specific (for example `ApplicationInsights` in `appsettings.json`) can remain at the root, but runtime configuration is read from the selected tenant block for each request.

## Resolution and usage
- Clients must send the `X-Tenant-ID` header (GUID) with every request. The middleware resolves the tenant and hydrates a scoped `ITenantContextAccessor` for downstream services.
- CORS policies, SignalR, and the EF Core DbContext pick connection strings and allowed origins from the resolved tenant entry.
- Requests with missing or unknown tenants return `400` with a short error payload.

## Operational notes
- Keep secrets (passwords, keys) in KeyVault or environment variables; reference them from the tenant sections where needed.
- When adding a new tenant, copy an existing configuration block into the `Tenants` section, update the GUID key/`Id`, `Name`, and per-tenant settings, then redeploy.
