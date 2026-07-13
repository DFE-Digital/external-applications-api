# Import Web tenant configuration into TenantConfig DB
#
# Prerequisites
# - Windows PowerShell 5.1+ or PowerShell 7+
# - API running (for upserts)
# - Tenant row already exists in TenantConfig
# - Bearer token for any authenticated API caller (admin endpoints are [Authorize] today)
#
# Dry run (merge + list categories, no writes):
#   powershell -File ./Import-WebTenantConfig.ps1 -DryRun
#
# Import settings:
#   $token = "<paste access token>"
#   powershell -File ./Import-WebTenantConfig.ps1 -AccessToken $token
#
# Import + ensure localhost hostname / frontend origin:
#   powershell -File ./Import-WebTenantConfig.ps1 -AccessToken $token `
#     -SqlConnectionString "Server=localhost,1433;Database=TenantConfig;User Id=SA;Password=YourPassword123!;TrustServerCertificate=True;"
#
# Other apps (e.g. Lsrp):
#   powershell -File ./Import-WebTenantConfig.ps1 -ApplicationName Lsrp `
#     -TenantId "22222222-2222-4222-8222-222222222222" `
#     -AccessToken $token
#
# After import, refresh API cache (X-Tenant-ID is required by TenantResolutionMiddleware):
#   Invoke-RestMethod -Method Post -Uri "https://localhost:7089/v1/admin/tenants/refresh" `
#     -Headers @{
#       Authorization = "Bearer $token"
#       "X-Tenant-ID" = "11111111-1111-4111-8111-111111111111"
#     }
