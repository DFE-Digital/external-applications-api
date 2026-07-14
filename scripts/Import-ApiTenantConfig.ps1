<#
.SYNOPSIS
  Imports API tenant settings (Tenants:{ApplicationName}) into TenantConfig as Target=Api.

.DESCRIPTION
  Loads the tenant object for Transfers / Lsrp / RGVisits from (in order, deep-merged):
    1) DfE.ExternalApplications.Api/appsettings.json  -> Tenants:{ApplicationName}
    2) appsettings.{Environment}.json                 -> Tenants:{ApplicationName} (optional)
    3) Optional -TenantSettingsFile (full tenant JSON object, or { "Tenants": { "App": { ... } } })
    4) API user secrets                               -> Tenants:{ApplicationName}

  Then upserts each top-level category (except Id/Name/Web/Hostnames/Frontend) via:
    PUT /v1/admin/tenants/{tenantId}/settings  with target=Api

  Optionally ensures the tenant row (and hostname/origin) via SQL.

  Compatible with Windows PowerShell 5.1 and PowerShell 7+.

.EXAMPLE
  .\Import-ApiTenantConfig.ps1 -ApplicationName Transfers -DryRun

.EXAMPLE
  $token = "<access token>"
  .\Import-ApiTenantConfig.ps1 -ApplicationName Lsrp -AccessToken $token

.EXAMPLE
  .\Import-ApiTenantConfig.ps1 -ApplicationName Visits -AccessToken $token `
    -SqlConnectionString "Server=localhost,1433;Database=TenantConfig;User Id=SA;Password=YourPassword123!;TrustServerCertificate=True;" `
    -Hostname "rgvisits.localhost" `
    -FrontendOrigin "https://rgvisits.localhost:7020"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Transfers", "Lsrp", "RGVisits", "Visits")]
    [string] $ApplicationName = "Transfers",

    [string] $TenantId = "",

    [string] $ApiProjectPath = "",

    [string] $ApiBaseUrl = "https://localhost:7089",

    [string] $AccessToken = "",

    [string] $Environment = "Development",

    # Optional: path to a JSON file containing the tenant settings object
    [string] $TenantSettingsFile = "",

    [string] $Hostname = "",

    [string] $FrontendOrigin = "",

    [string] $SqlConnectionString = "",

    [string[]] $SkipCategories = @(
        "Id",
        "Name",
        "Web",
        "Hostnames",
        "Frontend"
    ),

    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Allow local HTTPS with self-signed certs (Windows PowerShell 5.1)
try {
    add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicyApiImport : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicyApiImport
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}
catch {
    # Already registered or not needed
}

$script:KnownTenantIds = @{
    Transfers = "11111111-1111-4111-8111-111111111111"
    Lsrp      = "22222222-2222-4222-8222-222222222222"
    RGVisits  = "33333333-3333-4333-8333-333333333333"
    Visits    = "33333333-3333-4333-8333-333333333333"
}

$script:SecretCategoryNames = @(
    "ConnectionStrings",
    "AzureAd",
    "InternalServiceAuth",
    "Authorization",
    "DfESignIn",
    "Email",
    "FileStorage",
    "NotificationService"
)

function Resolve-ApplicationKey {
    param([string] $Name)
    if ($Name -eq "Visits") { return "RGVisits" }
    return $Name
}

function Resolve-TenantId {
    param(
        [string] $ApplicationName,
        [string] $TenantId
    )

    if (-not [string]::IsNullOrWhiteSpace($TenantId)) {
        return [Guid]$TenantId
    }

    $key = Resolve-ApplicationKey -Name $ApplicationName
    if (-not $script:KnownTenantIds.ContainsKey($key) -and -not $script:KnownTenantIds.ContainsKey($ApplicationName)) {
        throw "No default TenantId for '$ApplicationName'. Pass -TenantId explicitly."
    }

    $id = $script:KnownTenantIds[$ApplicationName]
    if ([string]::IsNullOrWhiteSpace($id)) {
        $id = $script:KnownTenantIds[$key]
    }
    return [Guid]$id
}

function Test-IsSecretCategory {
    param([string] $Name)
    return $script:SecretCategoryNames -contains $Name
}

function Resolve-ApiProjectPath {
    param([string] $Path)

    if (-not [string]::IsNullOrWhiteSpace($Path) -and (Test-Path $Path)) {
        return (Resolve-Path $Path).Path
    }

    $candidates = @(
        (Join-Path $PSScriptRoot "..\src\DfE.ExternalApplications.Api"),
        (Join-Path $PSScriptRoot "..\DfE.ExternalApplications.Api"),
        "c:\Users\FDASHTI\source\repos\external-applications-api\src\DfE.ExternalApplications.Api"
    )

    foreach ($candidate in $candidates) {
        $full = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path (Join-Path $full "DfE.ExternalApplications.Api.csproj")) {
            return $full
        }
    }

    throw "Could not locate DfE.ExternalApplications.Api. Pass -ApiProjectPath."
}

function Get-UserSecretsId {
    param([string] $ProjectPath)

    $csproj = Join-Path $ProjectPath "DfE.ExternalApplications.Api.csproj"
    [xml] $xml = Get-Content -Raw $csproj
    $id = $null
    foreach ($group in $xml.Project.PropertyGroup) {
        if ($group.UserSecretsId) {
            $id = [string]$group.UserSecretsId
            break
        }
    }
    if ([string]::IsNullOrWhiteSpace($id)) {
        throw "UserSecretsId not found in $csproj"
    }
    return $id.Trim()
}

function Get-UserSecretsPath {
    param([string] $UserSecretsId)

    $appData = $env:APPDATA
    if ([string]::IsNullOrWhiteSpace($appData)) {
        return Join-Path $HOME ".microsoft/usersecrets/$UserSecretsId/secrets.json"
    }
    return Join-Path $appData "Microsoft\UserSecrets\$UserSecretsId\secrets.json"
}

function ConvertTo-HashtableDeep {
    param($InputObject)

    if ($null -eq $InputObject) {
        return $null
    }

    if ($InputObject -is [hashtable]) {
        $copy = @{}
        foreach ($key in @($InputObject.Keys)) {
            $copy[$key] = ConvertTo-HashtableDeep $InputObject[$key]
        }
        return $copy
    }

    if ($InputObject -is [System.Management.Automation.PSCustomObject]) {
        $copy = @{}
        foreach ($prop in $InputObject.PSObject.Properties) {
            $copy[$prop.Name] = ConvertTo-HashtableDeep $prop.Value
        }
        return $copy
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $copy = @{}
        foreach ($key in @($InputObject.Keys)) {
            $copy["$key"] = ConvertTo-HashtableDeep $InputObject[$key]
        }
        return $copy
    }

    if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
        $list = New-Object System.Collections.ArrayList
        foreach ($item in $InputObject) {
            [void]$list.Add((ConvertTo-HashtableDeep $item))
        }
        return ,$list.ToArray()
    }

    return $InputObject
}

function Merge-HashtablesDeep {
    param(
        [hashtable] $Base,
        [hashtable] $Overlay
    )

    $result = ConvertTo-HashtableDeep $Base
    if ($null -eq $result) {
        $result = @{}
    }
    if ($null -eq $Overlay) {
        return $result
    }

    foreach ($key in @($Overlay.Keys)) {
        $overlayValue = $Overlay[$key]
        if ($result.ContainsKey($key) -and
            ($result[$key] -is [hashtable]) -and
            ($overlayValue -is [hashtable])) {
            $result[$key] = Merge-HashtablesDeep -Base $result[$key] -Overlay $overlayValue
        }
        else {
            $result[$key] = ConvertTo-HashtableDeep $overlayValue
        }
    }

    return $result
}

function Read-JsonFileAsHashtable {
    param([string] $Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    $raw = Get-Content -Raw -Path $Path
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }

    $obj = $raw | ConvertFrom-Json
    return ConvertTo-HashtableDeep $obj
}

function Get-TenantSectionFromRoot {
    param(
        [hashtable] $Root,
        [string] $ApplicationKey
    )

    if ($null -eq $Root) {
        return $null
    }

    # Direct tenant object file (categories at root, with Id/Name)
    if ($Root.ContainsKey("ConnectionStrings") -or $Root.ContainsKey("Authorization")) {
        if (-not $Root.ContainsKey("Tenants")) {
            return $Root
        }
    }

    if (-not $Root.ContainsKey("Tenants") -or -not ($Root["Tenants"] -is [hashtable])) {
        return $null
    }

    $tenants = $Root["Tenants"]
    $candidates = @($ApplicationKey)
    if ($ApplicationKey -eq "RGVisits") { $candidates += "Visits" }
    if ($ApplicationKey -eq "Visits") { $candidates += "RGVisits" }

    foreach ($name in $candidates) {
        if ($tenants.ContainsKey($name) -and ($tenants[$name] -is [hashtable])) {
            return $tenants[$name]
        }
    }

    return $null
}

function ConvertTo-SettingsJson {
    param($Value)

    if ($null -eq $Value) {
        return "null"
    }

    return ($Value | ConvertTo-Json -Compress -Depth 100)
}

function Ensure-SqlTenantExtras {
    param(
        [string] $ConnectionString,
        [Guid] $TenantId,
        [string] $TenantName,
        [string] $Hostname,
        [string] $FrontendOrigin
    )

    Write-Host "Ensuring tenant/hostname/origin via SQL..." -ForegroundColor Cyan

    $queries = New-Object System.Collections.Generic.List[string]
    [void]$queries.Add(@"
IF NOT EXISTS (SELECT 1 FROM tenantconfig.Tenants WHERE Id = '$TenantId')
BEGIN
  INSERT INTO tenantconfig.Tenants (Id, Name, IsActive, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('$TenantId', N'$TenantName', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
"@)

    if (-not [string]::IsNullOrWhiteSpace($Hostname)) {
        [void]$queries.Add(@"
IF NOT EXISTS (
  SELECT 1 FROM tenantconfig.TenantHostnames
  WHERE TenantId = '$TenantId' AND Hostname = N'$Hostname')
BEGIN
  INSERT INTO tenantconfig.TenantHostnames (Id, TenantId, Hostname)
  VALUES (NEWID(), '$TenantId', N'$Hostname');
END
"@)
    }

    if (-not [string]::IsNullOrWhiteSpace($FrontendOrigin)) {
        [void]$queries.Add(@"
IF NOT EXISTS (
  SELECT 1 FROM tenantconfig.TenantFrontendOrigins
  WHERE TenantId = '$TenantId' AND Origin = N'$FrontendOrigin')
BEGIN
  INSERT INTO tenantconfig.TenantFrontendOrigins (Id, TenantId, Origin)
  VALUES (NEWID(), '$TenantId', N'$FrontendOrigin');
END
"@)
    }

    $batch = ($queries -join "`n")

    if (-not (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue)) {
        throw "SqlServer PowerShell module not found (Invoke-Sqlcmd). Install with: Install-Module SqlServer -Scope CurrentUser. Or omit -SqlConnectionString."
    }

    Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $batch | Out-Null
    Write-Host "  Tenant/hostname/origin ensure complete." -ForegroundColor Green
}

function Invoke-UpsertSetting {
    param(
        [string] $ApiBaseUrl,
        [string] $AccessToken,
        [Guid] $TenantId,
        [string] $Category,
        [string] $SettingsJson,
        [bool] $IsSecret
    )

    $uri = "$($ApiBaseUrl.TrimEnd('/'))/v1/admin/tenants/$TenantId/settings"
    $bodyObj = @{
        category     = $Category
        target       = "Api"
        settingsJson = $SettingsJson
        isSecret     = $IsSecret
    }
    $body = $bodyObj | ConvertTo-Json -Compress

    $headers = @{
        Authorization = "Bearer $AccessToken"
        "X-Tenant-ID" = $TenantId.ToString()
    }

    try {
        $response = Invoke-WebRequest -Uri $uri -Method Put -Headers $headers -Body $body -ContentType "application/json" -UseBasicParsing
        return [pscustomobject]@{
            Category   = $Category
            StatusCode = [int]$response.StatusCode
            Ok         = $true
            Error      = $null
        }
    }
    catch {
        $status = $null
        $errorBody = $_.Exception.Message
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
            }
            catch { }
        }

        return [pscustomobject]@{
            Category   = $Category
            StatusCode = $status
            Ok         = $false
            Error      = $errorBody
        }
    }
}

# --- Main ---

$applicationKey = Resolve-ApplicationKey -Name $ApplicationName
$resolvedTenantId = Resolve-TenantId -ApplicationName $ApplicationName -TenantId $TenantId
$apiProject = Resolve-ApiProjectPath -Path $ApiProjectPath

Write-Host "API project:     $apiProject" -ForegroundColor Cyan
Write-Host "Application:     $ApplicationName (key=$applicationKey)" -ForegroundColor Cyan
Write-Host "TenantId:        $resolvedTenantId" -ForegroundColor Cyan
Write-Host "Target:          Api" -ForegroundColor Cyan

$merged = @{}

$baseSettingsPath = Join-Path $apiProject "appsettings.json"
$baseRoot = Read-JsonFileAsHashtable -Path $baseSettingsPath
$baseTenant = Get-TenantSectionFromRoot -Root $baseRoot -ApplicationKey $applicationKey
if ($null -ne $baseTenant) {
    Write-Host "Base settings:   $baseSettingsPath (Tenants:$applicationKey)" -ForegroundColor Cyan
    $merged = Merge-HashtablesDeep -Base $merged -Overlay $baseTenant
}
else {
    Write-Host "Base settings:   (no Tenants:$applicationKey in appsettings.json)" -ForegroundColor DarkGray
}

$envSettingsPath = Join-Path $apiProject "appsettings.$Environment.json"
$envRoot = Read-JsonFileAsHashtable -Path $envSettingsPath
$envTenant = Get-TenantSectionFromRoot -Root $envRoot -ApplicationKey $applicationKey
if ($null -ne $envTenant) {
    Write-Host "Env overlay:     $envSettingsPath (Tenants:$applicationKey)" -ForegroundColor Cyan
    $merged = Merge-HashtablesDeep -Base $merged -Overlay $envTenant
}
else {
    Write-Host "Env overlay:     (none)" -ForegroundColor DarkGray
}

if (-not [string]::IsNullOrWhiteSpace($TenantSettingsFile)) {
    if (-not (Test-Path $TenantSettingsFile)) {
        throw "TenantSettingsFile not found: $TenantSettingsFile"
    }
    $fileRoot = Read-JsonFileAsHashtable -Path $TenantSettingsFile
    $fileTenant = Get-TenantSectionFromRoot -Root $fileRoot -ApplicationKey $applicationKey
    if ($null -eq $fileTenant) {
        throw "Could not find tenant settings in $TenantSettingsFile (expected Tenants:$applicationKey or a tenant object at root)."
    }
    Write-Host "Settings file:   $TenantSettingsFile" -ForegroundColor Cyan
    $merged = Merge-HashtablesDeep -Base $merged -Overlay $fileTenant
}

$userSecretsId = Get-UserSecretsId -ProjectPath $apiProject
$userSecretsPath = Get-UserSecretsPath -UserSecretsId $userSecretsId
$secretsRoot = Read-JsonFileAsHashtable -Path $userSecretsPath
$secretsTenant = Get-TenantSectionFromRoot -Root $secretsRoot -ApplicationKey $applicationKey
if ($null -ne $secretsTenant) {
    Write-Host "User secrets:    $userSecretsPath (Tenants:$applicationKey)" -ForegroundColor Cyan
    $merged = Merge-HashtablesDeep -Base $merged -Overlay $secretsTenant
}
else {
    Write-Host "User secrets:    (no Tenants:$applicationKey at $userSecretsPath)" -ForegroundColor Yellow
}

if ($merged.Keys.Count -eq 0) {
    throw @"
No API tenant settings found for '$applicationKey'.

Expected one of:
  - API user secrets: Tenants:$applicationKey { ConnectionStrings, Authorization, ... }
  - API appsettings*.json Tenants:$applicationKey
  - -TenantSettingsFile pointing at a tenant JSON object

Check secrets with:  dotnet user-secrets list --project `"$apiProject`"
"@
}

$categories = @()
foreach ($key in ($merged.Keys | Sort-Object)) {
    if ($SkipCategories -contains $key) {
        continue
    }

    $value = $merged[$key]
    $settingsJson = ConvertTo-SettingsJson -Value $value
    if ([string]::IsNullOrWhiteSpace($settingsJson) -or $settingsJson -eq "{}") {
        continue
    }

    $preview = $settingsJson
    if ($preview.Length -gt 120) {
        $preview = $preview.Substring(0, 117) + "..."
    }

    $categories += [pscustomobject]@{
        Category     = $key
        IsSecret     = (Test-IsSecretCategory -Name $key)
        SettingsJson = $settingsJson
        Preview      = $preview
    }
}

Write-Host ""
Write-Host "Categories to upsert (Target=Api): $($categories.Count)" -ForegroundColor Cyan
$categories | Format-Table Category, IsSecret, Preview -AutoSize

if ($categories.Count -eq 0) {
    throw "Nothing to upsert after applying SkipCategories."
}

if ($DryRun) {
    Write-Host "DryRun: no API or SQL changes made." -ForegroundColor Yellow
    return
}

if (-not [string]::IsNullOrWhiteSpace($SqlConnectionString)) {
    Ensure-SqlTenantExtras `
        -ConnectionString $SqlConnectionString `
        -TenantId $resolvedTenantId `
        -TenantName $applicationKey `
        -Hostname $Hostname `
        -FrontendOrigin $FrontendOrigin
}

if ([string]::IsNullOrWhiteSpace($AccessToken)) {
    throw "AccessToken is required to upsert settings (admin API is [Authorize]). Pass -AccessToken or use -DryRun."
}

Write-Host ""
Write-Host "Upserting via $ApiBaseUrl ..." -ForegroundColor Cyan

$results = @()
foreach ($item in $categories) {
    $result = Invoke-UpsertSetting `
        -ApiBaseUrl $ApiBaseUrl `
        -AccessToken $AccessToken `
        -TenantId $resolvedTenantId `
        -Category $item.Category `
        -SettingsJson $item.SettingsJson `
        -IsSecret ([bool]$item.IsSecret)

    $results += $result
    if ($result.Ok) {
        Write-Host "  OK  $($item.Category) ($($result.StatusCode))" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL $($item.Category) ($($result.StatusCode)): $($result.Error)" -ForegroundColor Red
    }
}

$failed = @($results | Where-Object { -not $_.Ok })
$succeeded = $results.Count - $failed.Count
Write-Host ""
Write-Host "Done. Succeeded: $succeeded  Failed: $($failed.Count)" -ForegroundColor $(if ($failed.Count) { "Yellow" } else { "Green" })

if ($failed.Count -gt 0) {
    Write-Host "Tip: after upserts, call POST /v1/admin/tenants/refresh so the API cache reloads." -ForegroundColor Cyan
    exit 1
}

Write-Host ("Next: POST {0}/v1/admin/tenants/refresh then verify TenantSettings Target=Api for {1}" -f $ApiBaseUrl, $resolvedTenantId) -ForegroundColor Cyan
Write-Host @"

Refresh example:
  Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/v1/admin/tenants/refresh" ``
    -Headers @{ Authorization = "Bearer <token>"; "X-Tenant-ID" = "$resolvedTenantId" }
"@ -ForegroundColor Cyan
