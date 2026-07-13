<#
.SYNOPSIS
  Imports Web folder appsettings (+ user secrets) into TenantConfig as Target=Web settings.

.DESCRIPTION
  Loads:
    1) configurations/{ApplicationName}/appsettings.json
    2) configurations/{ApplicationName}/appsettings.{Environment}.json (optional)
    3) Web user secrets (UserSecretsId from the Web csproj), merging the
       application-named section (e.g. "Transfers") onto the root, same as Program.cs

  Then upserts each top-level category via:
    PUT /v1/admin/tenants/{tenantId}/settings

  Optionally ensures TenantHostnames / FrontendOrigins via SQL.

  Compatible with Windows PowerShell 5.1 and PowerShell 7+.

.EXAMPLE
  .\Import-WebTenantConfig.ps1 -DryRun

.EXAMPLE
  .\Import-WebTenantConfig.ps1 -AccessToken $token

.EXAMPLE
  .\Import-WebTenantConfig.ps1 -AccessToken $token `
    -SqlConnectionString "Server=localhost,1433;Database=TenantConfig;User Id=SA;Password=YourPassword123!;TrustServerCertificate=True;"
#>
[CmdletBinding()]
param(
    [string] $ApplicationName = "Transfers",

    [Guid] $TenantId = "11111111-1111-4111-8111-111111111111",

    [string] $WebProjectPath = "",

    [string] $ApiBaseUrl = "https://localhost:7089",

    [string] $AccessToken = "",

    [string] $Environment = "Development",

    [string] $Hostname = "localhost",

    [string] $FrontendOrigin = "https://localhost:7020",

    [string] $SqlConnectionString = "",

    [string[]] $SkipCategories = @(
        "AllowedHosts",
        "Logging",
        "DetailedErrors"
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
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}
catch {
    # Already registered or not needed
}

$script:SecretCategoryNames = @(
    "ConnectionStrings",
    "AzureAd",
    "InternalServiceAuth",
    "DfESignIn",
    "ExternalApplicationsApiClient",
    "TokenRefresh",
    "CacheSettings",
    "EntraSso"
)

function Test-IsSecretCategory {
    param([string] $Name)
    return $script:SecretCategoryNames -contains $Name
}

function Resolve-WebProjectPath {
    param([string] $Path)

    if (-not [string]::IsNullOrWhiteSpace($Path) -and (Test-Path $Path)) {
        return (Resolve-Path $Path).Path
    }

    $candidates = @(
        (Join-Path $PSScriptRoot "..\..\external-applications-web\src\DfE.ExternalApplications.Web"),
        (Join-Path $PSScriptRoot "..\..\..\external-applications-web\src\DfE.ExternalApplications.Web"),
        "c:\Users\FDASHTI\source\repos\external-applications-web\src\DfE.ExternalApplications.Web"
    )

    foreach ($candidate in $candidates) {
        $full = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path (Join-Path $full "DfE.ExternalApplications.Web.csproj")) {
            return $full
        }
    }

    throw "Could not locate DfE.ExternalApplications.Web. Pass -WebProjectPath."
}

function Get-UserSecretsId {
    param([string] $ProjectPath)

    $csproj = Join-Path $ProjectPath "DfE.ExternalApplications.Web.csproj"
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

    # PSCustomObject from ConvertFrom-Json (Windows PowerShell 5.1)
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
        [string] $Hostname,
        [string] $FrontendOrigin
    )

    Write-Host "Ensuring hostname/origin via SQL..." -ForegroundColor Cyan

    $queries = New-Object System.Collections.Generic.List[string]
    [void]$queries.Add(@"
IF NOT EXISTS (SELECT 1 FROM tenantconfig.Tenants WHERE Id = '$TenantId')
BEGIN
  RAISERROR('Tenant $TenantId does not exist. Create the tenant row first.', 16, 1);
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
        throw "SqlServer PowerShell module not found (Invoke-Sqlcmd). Install with: Install-Module SqlServer -Scope CurrentUser. Or omit -SqlConnectionString and insert hostname/origin manually."
    }

    Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $batch | Out-Null
    Write-Host "  Hostname/origin ensure complete." -ForegroundColor Green
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
        target       = "Web"
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

$webProject = Resolve-WebProjectPath -Path $WebProjectPath
$configurationsPath = Join-Path $webProject "configurations\$ApplicationName"
$baseSettingsPath = Join-Path $configurationsPath "appsettings.json"
$envSettingsPath = Join-Path $configurationsPath "appsettings.$Environment.json"

if (-not (Test-Path $baseSettingsPath)) {
    throw "Base settings not found: $baseSettingsPath"
}

Write-Host "Web project:     $webProject" -ForegroundColor Cyan
Write-Host "Application:     $ApplicationName" -ForegroundColor Cyan
Write-Host "TenantId:        $TenantId" -ForegroundColor Cyan
Write-Host "Base settings:   $baseSettingsPath" -ForegroundColor Cyan

$merged = Read-JsonFileAsHashtable -Path $baseSettingsPath
if ($null -eq $merged) {
    throw "Failed to parse $baseSettingsPath"
}

$envOverlay = Read-JsonFileAsHashtable -Path $envSettingsPath
if ($null -ne $envOverlay) {
    Write-Host "Env overlay:     $envSettingsPath" -ForegroundColor Cyan
    $merged = Merge-HashtablesDeep -Base $merged -Overlay $envOverlay
}
else {
    Write-Host "Env overlay:     (none)" -ForegroundColor DarkGray
}

$userSecretsId = Get-UserSecretsId -ProjectPath $webProject
$userSecretsPath = Get-UserSecretsPath -UserSecretsId $userSecretsId
$secrets = Read-JsonFileAsHashtable -Path $userSecretsPath

if ($null -ne $secrets) {
    Write-Host "User secrets:    $userSecretsPath" -ForegroundColor Cyan

    # Match Web Program.cs: only bind the application-named section onto root
    if ($secrets.ContainsKey($ApplicationName) -and ($secrets[$ApplicationName] -is [hashtable])) {
        $merged = Merge-HashtablesDeep -Base $merged -Overlay $secrets[$ApplicationName]
        Write-Host "  Merged section '$ApplicationName' from user secrets." -ForegroundColor Green
    }
    else {
        Write-Host "  No '$ApplicationName' section in user secrets; secrets not overlaid." -ForegroundColor Yellow
    }
}
else {
    Write-Host "User secrets:    (not found at $userSecretsPath)" -ForegroundColor Yellow
}

# Skip sibling application sections if they leaked into merged config
$configurationsRoot = Join-Path $webProject "configurations"
if (Test-Path $configurationsRoot) {
    $siblingApps = @(Get-ChildItem -Path $configurationsRoot -Directory | ForEach-Object { $_.Name })
    foreach ($sibling in $siblingApps) {
        if ($sibling -ne $ApplicationName -and ($SkipCategories -notcontains $sibling)) {
            $SkipCategories += $sibling
        }
    }
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
Write-Host "Categories to upsert (Target=Web): $($categories.Count)" -ForegroundColor Cyan
$categories | Format-Table Category, IsSecret, Preview -AutoSize

if ($DryRun) {
    Write-Host "DryRun: no API or SQL changes made." -ForegroundColor Yellow
    return
}

if (-not [string]::IsNullOrWhiteSpace($SqlConnectionString)) {
    Ensure-SqlTenantExtras `
        -ConnectionString $SqlConnectionString `
        -TenantId $TenantId `
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
        -TenantId $TenantId `
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

Write-Host ("Next: POST {0}/v1/admin/tenants/refresh then GET /v1/tenant-config/tenants/{1}?target=Web" -f $ApiBaseUrl, $TenantId) -ForegroundColor Cyan
