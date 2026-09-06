@description('Azure Key Vault — store of record for app secrets and DB connection strings. Secret values and RBAC are set out of band, not in this module.')
param name string
param location string = resourceGroup().location
param tags object = {}

@description('Soft-delete retention in days (7–90).')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 90

@description('Purge protection cannot be disabled later.')
param enablePurgeProtection bool = false

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

output id string = keyVault.id
output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
