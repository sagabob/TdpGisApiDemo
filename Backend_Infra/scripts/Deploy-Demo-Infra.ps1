#Requires -Version 5.1
<#
.SYNOPSIS
  Demo deploy helper: Key Vault bootstrap, secrets from .env, then Container Apps.

.EXAMPLE
  .\Deploy-Demo-Infra.ps1 -Phase Bootstrap
  .\Deploy-Demo-Infra.ps1 -Phase Secrets
  .\Deploy-Demo-Infra.ps1 -Phase Apps
  .\Deploy-Demo-Infra.ps1 -Phase All
#>
[CmdletBinding()]
param(
  [ValidateSet('Bootstrap', 'Secrets', 'Apps', 'All')]
  [string] $Phase = 'All',

  [string] $ResourceGroupName = 'rg-tdpgis-demo',
  [string] $Location = 'australiaeast',
  [string] $SubscriptionId,
  [string] $OfficerObjectId,
  [string] $EnvFile,
  [switch] $SkipWhatIf
)

$ErrorActionPreference = 'Stop'
$InfraRoot = Split-Path -Parent $PSScriptRoot
$BootstrapBicep = Join-Path $InfraRoot 'bootstrap.bicep'
$BootstrapParams = Join-Path $InfraRoot 'bootstrap.bicepparam'
$MainBicep = Join-Path $InfraRoot 'main.bicep'
$MainParams = Join-Path $InfraRoot 'main.bicepparam'
if (-not $EnvFile) {
  $EnvFile = Join-Path $InfraRoot '.env'
}

function Import-DotEnv {
  param([Parameter(Mandatory)] [string] $Path)
  if (-not (Test-Path -LiteralPath $Path)) {
    return $false
  }

  Get-Content -LiteralPath $Path | ForEach-Object {
    $line = $_.Trim()
    if ($line -eq '' -or $line.StartsWith('#')) {
      return
    }
    $eq = $line.IndexOf('=')
    if ($eq -lt 1) {
      return
    }
    $key = $line.Substring(0, $eq).Trim()
    $value = $line.Substring($eq + 1).Trim()
    if (
      ($value.StartsWith('"') -and $value.EndsWith('"')) -or
      ($value.StartsWith("'") -and $value.EndsWith("'"))
    ) {
      $value = $value.Substring(1, $value.Length - 2)
    }
    Set-Item -Path "Env:$key" -Value $value
  }

  Write-Host "Loaded secrets file: $Path"
  return $true
}

function Get-BicepParamValue {
  param(
    [Parameter(Mandatory)] [string] $Path,
    [Parameter(Mandatory)] [string] $Name
  )
  $line = Select-String -Path $Path -Pattern "^\s*param\s+$Name\s*=\s*'([^']*)'" | Select-Object -First 1
  if (-not $line) {
    throw "Could not read param '$Name' from $Path"
  }
  return $line.Matches[0].Groups[1].Value
}

function ConvertFrom-SecureText {
  param([Parameter(Mandatory)] [System.Security.SecureString] $Secure)
  $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
  try {
    return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
  }
  finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
  }
}

function Invoke-Az {
  param([Parameter(Mandatory)] [string[]] $AzArgs)
  & az @AzArgs
  if ($LASTEXITCODE -ne 0) {
    throw "az $($AzArgs -join ' ') failed with exit code $LASTEXITCODE"
  }
}

function Test-AzLoggedIn {
  az account show --output none 2>$null
  return ($LASTEXITCODE -eq 0)
}

function Initialize-AzureContext {
  if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI (az) is not on PATH.'
  }

  if (-not (Test-AzLoggedIn)) {
    Write-Host 'Not logged in. Opening az login...'
    Invoke-Az @('login')
  }

  if ($SubscriptionId) {
    Invoke-Az @('account', 'set', '--subscription', $SubscriptionId)
  }

  $account = Invoke-Az @('account', 'show', '--output', 'json') | ConvertFrom-Json
  Write-Host "Subscription: $($account.name) ($($account.id))"
  az bicep version --output none 2>$null
  if ($LASTEXITCODE -ne 0) {
    Invoke-Az @('bicep', 'install')
  }
}

function Get-OfficerObjectId {
  if ($OfficerObjectId) {
    return $OfficerObjectId
  }
  $id = (az ad signed-in-user show --query id -o tsv 2>$null)
  if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($id)) {
    throw 'Could not resolve signed-in user. Pass -OfficerObjectId (your Entra object ID).'
  }
  return $id.Trim()
}

function Get-FirstEnvValue {
  param([string[]] $Names)
  foreach ($name in $Names) {
    if ([string]::IsNullOrWhiteSpace($name)) {
      continue
    }
    $value = [Environment]::GetEnvironmentVariable($name)
    if (-not [string]::IsNullOrWhiteSpace($value)) {
      return $value
    }
  }
  return $null
}

function Read-OptionalHost {
  param(
    [Parameter(Mandatory)] [string] $Prompt,
    [string] $Default,
    [string[]] $EnvironmentVariable
  )
  $fromEnv = Get-FirstEnvValue -Names $EnvironmentVariable
  if ($fromEnv) {
    return $fromEnv
  }
  $suffix = if ($Default) { " [$Default]" } else { '' }
  $value = Read-Host "$Prompt$suffix"
  if ([string]::IsNullOrWhiteSpace($value)) {
    return $Default
  }
  return $value
}

function Read-RequiredHost {
  param(
    [Parameter(Mandatory)] [string] $Prompt,
    [string[]] $EnvironmentVariable,
    [switch] $Secret
  )
  $fromEnv = Get-FirstEnvValue -Names $EnvironmentVariable
  if ($fromEnv) {
    return $fromEnv
  }
  if ($Secret) {
    $secure = Read-Host $Prompt -AsSecureString
    $text = ConvertFrom-SecureText -Secure $secure
    if ([string]::IsNullOrWhiteSpace($text)) {
      throw "A value is required for: $Prompt"
    }
    return $text
  }
  $value = Read-Host $Prompt
  if ([string]::IsNullOrWhiteSpace($value)) {
    throw "A value is required for: $Prompt"
  }
  return $value
}

function Set-VaultSecret {
  param(
    [Parameter(Mandatory)] [string] $VaultName,
    [Parameter(Mandatory)] [string] $Name,
    [Parameter(Mandatory)] [string] $Value
  )
  $tmp = New-TemporaryFile
  try {
    [System.IO.File]::WriteAllText($tmp.FullName, $Value)
    Invoke-Az @('keyvault', 'secret', 'set', '--vault-name', $VaultName, '--name', $Name, '--file', $tmp.FullName, '--output', 'none')
    Write-Host "  set $Name"
  }
  finally {
    Remove-Item -Path $tmp.FullName -Force -ErrorAction SilentlyContinue
  }
}

function Wait-VaultAccess {
  param([Parameter(Mandatory)] [string] $VaultName)
  $deadline = (Get-Date).AddMinutes(3)
  Write-Host "Waiting for Key Vault RBAC on $VaultName ..."
  do {
    az keyvault secret list --vault-name $VaultName --output none 2>$null
    if ($LASTEXITCODE -eq 0) {
      Write-Host 'Key Vault access confirmed.'
      return
    }
    Start-Sleep -Seconds 10
  } while ((Get-Date) -lt $deadline)
  throw "No Key Vault access after 3 minutes. Wait and re-run: -Phase Secrets"
}

function Ensure-KeyVaultRole {
  param(
    [Parameter(Mandatory)] [string] $Scope,
    [Parameter(Mandatory)] [string] $ObjectId,
    [Parameter(Mandatory)] [string] $RoleName,
    [Parameter(Mandatory)] [string] $PrincipalType
  )
  $existing = az role assignment list `
    --scope $Scope `
    --assignee-object-id $ObjectId `
    --role $RoleName `
    --query '[].id' `
    -o tsv 2>$null
  if (-not [string]::IsNullOrWhiteSpace($existing)) {
    Write-Host "  already assigned: $RoleName"
    return
  }

  Invoke-Az @(
    'role', 'assignment', 'create',
    '--scope', $Scope,
    '--assignee-object-id', $ObjectId,
    '--assignee-principal-type', $PrincipalType,
    '--role', $RoleName,
    '--output', 'none'
  )
  Write-Host "  assigned: $RoleName"
}

function Invoke-Bootstrap {
  Write-Host "`n=== Phase 1: Key Vault bootstrap ==="
  Invoke-Az @('group', 'create', '--name', $ResourceGroupName, '--location', $Location, '--output', 'none')
  $officerId = Get-OfficerObjectId
  Write-Host "Secrets Officer: $officerId"

  Invoke-Az @(
    'deployment', 'group', 'create',
    '--resource-group', $ResourceGroupName,
    '--name', 'tdpgis-bootstrap',
    '--template-file', $BootstrapBicep,
    '--parameters', $BootstrapParams,
    '--output', 'none'
  )

  $outputs = Invoke-Az @(
    'deployment', 'group', 'show',
    '--resource-group', $ResourceGroupName,
    '--name', 'tdpgis-bootstrap',
    '--query', 'properties.outputs',
    '--output', 'json'
  ) | ConvertFrom-Json

  $vaultId = $outputs.keyVaultId.value
  $identityPrincipalId = $outputs.appsIdentityPrincipalId.value
  Write-Host "Key Vault: $($outputs.keyVaultNameOut.value)"
  Write-Host "Identity:  $($outputs.appsIdentityNameOut.value)"
  Write-Host 'Assigning Key Vault RBAC (skipped if already present)...'
  Ensure-KeyVaultRole -Scope $vaultId -ObjectId $officerId -RoleName 'Key Vault Secrets Officer' -PrincipalType 'User'
  Ensure-KeyVaultRole -Scope $vaultId -ObjectId $identityPrincipalId -RoleName 'Key Vault Secrets User' -PrincipalType 'ServicePrincipal'
}

function Invoke-Secrets {
  $vault = Get-BicepParamValue -Path $BootstrapParams -Name 'keyVaultName'
  $registryUser = Get-BicepParamValue -Path $MainParams -Name 'registryUsername'
  $needRegistryPassword = -not [string]::IsNullOrWhiteSpace($registryUser) -and $registryUser -notmatch '<.+>'

  Write-Host "`n=== Phase 2: Key Vault secrets ==="
  Write-Host "Vault: $vault"
  Write-Host 'Values are sent to Key Vault only (not Bicep). Sensitive prompts are hidden.'
  if (-not (Import-DotEnv -Path $EnvFile)) {
    Write-Host "No .env at $EnvFile — copy .env.example to .env, or you will be prompted."
  }
  Wait-VaultAccess -VaultName $vault

  $db = Read-RequiredHost -Prompt 'Postgres connection string' -EnvironmentVariable 'TDPGIS_DATABASE_CONNECTION_STRING' -Secret
  $apiTenant = Read-RequiredHost -Prompt 'API Entra tenant ID' -EnvironmentVariable 'TDPGIS_API_TENANT_ID'
  $apiClient = Read-RequiredHost -Prompt 'API app client ID' -EnvironmentVariable 'TDPGIS_API_CLIENT_ID'
  $apiAudience = Read-OptionalHost -Prompt 'API audience' -Default "api://$apiClient" -EnvironmentVariable 'TDPGIS_API_AUDIENCE'
  $apiRole = Read-OptionalHost -Prompt 'API access app role' -Default 'TdpGisApi.Access' -EnvironmentVariable @('TDPGIS_API_ACCESS_ROLE', 'TDPGIS_API_ACCESS_APP_ROLE')

  $epTenant = Read-OptionalHost -Prompt 'Endpoints Entra tenant ID' -Default $apiTenant -EnvironmentVariable 'TDPGIS_ENDPOINTS_TENANT_ID'
  $epClient = Read-RequiredHost -Prompt 'Endpoints (admin) app client ID' -EnvironmentVariable 'TDPGIS_ENDPOINTS_CLIENT_ID'
  $epSecret = Read-RequiredHost -Prompt 'Endpoints app client secret' -EnvironmentVariable 'TDPGIS_ENDPOINTS_CLIENT_SECRET' -Secret
  $epCallback = Read-OptionalHost -Prompt 'OIDC callback path' -Default '/signin-oidc' -EnvironmentVariable @('TDPGIS_ENDPOINTS_CALLBACK', 'TDPGIS_ENDPOINTS_CALLBACK_PATH')
  $epAdmin = Read-OptionalHost -Prompt 'Admin app role' -Default 'Gis.Admin' -EnvironmentVariable @('TDPGIS_ENDPOINTS_ADMIN_ROLE', 'TDPGIS_ENDPOINTS_ADMIN_APP_ROLE')
  $epViewer = Read-OptionalHost -Prompt 'Viewer app role' -Default 'Gis.Viewer' -EnvironmentVariable @('TDPGIS_ENDPOINTS_VIEWER_ROLE', 'TDPGIS_ENDPOINTS_VIEWER_APP_ROLE')

  $registryPassword = $null
  if ($needRegistryPassword) {
    $registryPassword = Read-RequiredHost -Prompt 'Registry password / token' -EnvironmentVariable 'TDPGIS_REGISTRY_PASSWORD' -Secret
  }

  Set-VaultSecret -VaultName $vault -Name 'database-connection-string' -Value $db
  Set-VaultSecret -VaultName $vault -Name 'api-azuread-tenant-id' -Value $apiTenant
  Set-VaultSecret -VaultName $vault -Name 'api-azuread-client-id' -Value $apiClient
  Set-VaultSecret -VaultName $vault -Name 'api-azuread-audience' -Value $apiAudience
  Set-VaultSecret -VaultName $vault -Name 'api-azuread-api-access-app-role' -Value $apiRole
  Set-VaultSecret -VaultName $vault -Name 'endpoints-azuread-tenant-id' -Value $epTenant
  Set-VaultSecret -VaultName $vault -Name 'endpoints-azuread-client-id' -Value $epClient
  Set-VaultSecret -VaultName $vault -Name 'endpoints-azuread-client-secret' -Value $epSecret
  Set-VaultSecret -VaultName $vault -Name 'endpoints-azuread-callback-path' -Value $epCallback
  Set-VaultSecret -VaultName $vault -Name 'endpoints-azuread-admin-app-role' -Value $epAdmin
  Set-VaultSecret -VaultName $vault -Name 'endpoints-azuread-viewer-app-role' -Value $epViewer
  if ($needRegistryPassword) {
    Set-VaultSecret -VaultName $vault -Name 'registry-password' -Value $registryPassword
  }

  Write-Host "`nSecret names in vault (values not shown):"
  Invoke-Az @('keyvault', 'secret', 'list', '--vault-name', $vault, '--query', '[].name', '--output', 'tsv')
}

function Invoke-Apps {
  Write-Host "`n=== Phase 3: Container Apps ==="
  $apiImage = Get-BicepParamValue -Path $MainParams -Name 'apiImage'
  if ($apiImage -match '<dockerhub-user>|<version>') {
    throw "Fill real image tags in $MainParams before deploying apps."
  }

  if (-not $SkipWhatIf) {
    Write-Host 'Running what-if...'
    Invoke-Az @(
      'deployment', 'group', 'what-if',
      '--resource-group', $ResourceGroupName,
      '--template-file', $MainBicep,
      '--parameters', $MainParams
    )
  }

  Invoke-Az @(
    'deployment', 'group', 'create',
    '--resource-group', $ResourceGroupName,
    '--name', 'tdpgis-apps',
    '--template-file', $MainBicep,
    '--parameters', $MainParams,
    '--output', 'none'
  )

  $outputs = Invoke-Az @(
    'deployment', 'group', 'show',
    '--resource-group', $ResourceGroupName,
    '--name', 'tdpgis-apps',
    '--query', 'properties.outputs',
    '--output', 'json'
  ) | ConvertFrom-Json

  Write-Host "`nApp outputs:"
  Write-Host "  apiUrl:        $($outputs.apiUrl.value)"
  Write-Host "  endpointsUrl:  $($outputs.endpointsUrl.value)"
  Write-Host "  keyVault:      $($outputs.keyVaultNameOut.value)"
  Write-Host "`nAdd this Entra redirect URI on the admin app registration:"
  Write-Host "  $($outputs.endpointsUrl.value)/signin-oidc"
}

Initialize-AzureContext

switch ($Phase) {
  'Bootstrap' { Invoke-Bootstrap }
  'Secrets' { Invoke-Secrets }
  'Apps' { Invoke-Apps }
  'All' {
    Invoke-Bootstrap
    Invoke-Secrets
    Invoke-Apps
  }
}

Write-Host "`nDone ($Phase)."
