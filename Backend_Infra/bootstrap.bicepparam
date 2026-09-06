using './bootstrap.bicep'

param location = 'australiaeast'
param namePrefix = 'tdpgis'
param environmentName = 'demo'

// Must be globally unique, 3–24 characters. Keep this exact name for main.bicepparam.
param appsIdentityName = 'id-tdpgis-containerapps-demo'
param keyVaultName = 'kv-tdpgis-demo-01'
param enablePurgeProtection = true
