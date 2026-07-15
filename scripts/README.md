# TenantConfig import scripts (Path 3 — migration / bootstrap only)
#
# These scripts seed TenantConfig from legacy per-app folders and secrets.
# They are NOT used by the runtime platform Web/API artefacts. Once tenant
# settings live in the TenantConfig database, prefer admin APIs / SQL for updates.
#
# Prerequisites
# - Windows PowerShell 5.1+ or PowerShell 7+
# - API running (for upserts)
# - Bearer token for an authenticated API caller (admin endpoints are [Authorize] today)
#
# -----------------------------------------------------------------------------
# Web settings (Target=Web) — Import-WebTenantConfig.ps1
# -----------------------------------------------------------------------------
# Reads external-applications-web configurations/{App}/appsettings*.json + Web user secrets
# (migration source only; those folders are not packaged into the Web image).
#
# Dry run:
#   powershell -File ./Import-WebTenantConfig.ps1 -DryRun
#
# Transfers:
#   powershell -File ./Import-WebTenantConfig.ps1 -AccessToken $token
#
# LSRP:
#   powershell -File ./Import-WebTenantConfig.ps1 -ApplicationName Lsrp `
#     -TenantId "22222222-2222-4222-8222-222222222222" `
#     -Hostname "lsrp.localhost" `
#     -FrontendOrigin "https://lsrp.localhost:7020" `
#     -AccessToken $token `
#     -SqlConnectionString "Server=localhost,1433;Database=TenantConfig;User Id=SA;Password=YourPassword123!;TrustServerCertificate=True;"
#
# RGVisits:
#   powershell -File ./Import-WebTenantConfig.ps1 -ApplicationName RGVisits `
#     -TenantId "33333333-3333-4333-8333-333333333333" `
#     -Hostname "rgvisits.localhost" `
#     -FrontendOrigin "https://rgvisits.localhost:7020" `
#     -AccessToken $token `
#     -SqlConnectionString "..."
#
# -----------------------------------------------------------------------------
# API settings (Target=Api) — Import-ApiTenantConfig.ps1
# -----------------------------------------------------------------------------
# Reads Tenants:{Transfers|Lsrp|RGVisits} from API appsettings*.json and/or API user secrets.
# Use -ApplicationName Transfers | Lsrp | RGVisits | Visits (Visits = RGVisits).
# Default TenantIds: Transfers=1111..., Lsrp=2222..., RGVisits=3333...
#
# Dry run:
#   powershell -File ./Import-ApiTenantConfig.ps1 -ApplicationName Lsrp -DryRun
#
# LSRP API settings:
#   powershell -File ./Import-ApiTenantConfig.ps1 -ApplicationName Lsrp -AccessToken $token `
#     -SqlConnectionString "Server=localhost,1433;Database=TenantConfig;User Id=SA;Password=YourPassword123!;TrustServerCertificate=True;" `
#     -Hostname "lsrp.localhost" `
#     -FrontendOrigin "https://lsrp.localhost:7020"
#
# RGVisits API settings:
#   powershell -File ./Import-ApiTenantConfig.ps1 -ApplicationName Visits -AccessToken $token `
#     -SqlConnectionString "..." `
#     -Hostname "rgvisits.localhost" `
#     -FrontendOrigin "https://rgvisits.localhost:7020"
#
# Optional JSON file (tenant object or { "Tenants": { "Lsrp": { ... } } }):
#   powershell -File ./Import-ApiTenantConfig.ps1 -ApplicationName Lsrp `
#     -TenantSettingsFile ".\lsrp-api-tenant.json" -AccessToken $token
#
# If user secrets have no Tenants:Lsrp section yet, copy from Transfers in secrets and
# change ConnectionStrings:DefaultConnection (and TokenSettings if different).
#
# -----------------------------------------------------------------------------
# After either import — refresh API cache
# -----------------------------------------------------------------------------
#   Invoke-RestMethod -Method Post -Uri "https://localhost:7089/v1/admin/tenants/refresh" `
#     -Headers @{
#       Authorization = "Bearer $token"
#       "X-Tenant-ID" = "22222222-2222-4222-8222-222222222222"
#     }
#
# Verify:
#   SELECT Category, Target FROM tenantconfig.TenantSettings
#   WHERE TenantId = '22222222-2222-4222-8222-222222222222'
#   ORDER BY Target, Category;
