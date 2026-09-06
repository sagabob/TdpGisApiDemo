targetScope = 'resourceGroup'

@description('Azure region for Key Vault and the app identity.')
param location string = resourceGroup().location

@description('Short name prefix used in resource names (letters/numbers only, lowercase preferred).')
@minLength(2)
@maxLength(16)
param namePrefix string = 'tdpgis'

@description('Environment label, e.g. demo.')
param environmentName string = 'demo'

@description('Resource tags applied to all resources.')
param tags object = {
  project: 'TdpGis'
  environment: environmentName
}

@description('User-assigned identity name used by Container Apps to read Key Vault. Empty generates id-<prefix>-containerapps-<env>.')
param appsIdentityName string = ''

@description('Key Vault name (3–24 chars, globally unique). Empty generates kv-<prefix>-<env>-<hash>.')
param keyVaultName string = ''

@description('Purge protection cannot be turned off later.')
param enablePurgeProtection bool = true

var resolvedIdentityName = empty(appsIdentityName) ? 'id-${namePrefix}-containerapps-${environmentName}' : appsIdentityName
var generatedKeyVaultName = take(
  'kv-${take(namePrefix, 6)}-${take(environmentName, 3)}-${uniqueString(resourceGroup().id)}',
  24
)
var resolvedKeyVaultName = empty(keyVaultName) ? generatedKeyVaultName : keyVaultName

module appsIdentity 'modules/managedIdentity.bicep' = {
  name: 'appsIdentity'
  params: {
    name: resolvedIdentityName
    location: location
    tags: tags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    name: resolvedKeyVaultName
    location: location
    tags: tags
    enablePurgeProtection: enablePurgeProtection
  }
}

output keyVaultId string = keyVault.outputs.id
output keyVaultNameOut string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output appsIdentityNameOut string = appsIdentity.outputs.name
output appsIdentityId string = appsIdentity.outputs.id
output appsIdentityPrincipalId string = appsIdentity.outputs.principalId
