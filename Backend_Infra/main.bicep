targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short name prefix used in resource names (letters/numbers only, lowercase preferred).')
@minLength(2)
@maxLength(16)
param namePrefix string = 'tdpgis'

@description('Environment label, e.g. demo. Must match bootstrap.')
param environmentName string = 'demo'

@description('Resource tags applied to all resources.')
param tags object = {
  project: 'TdpGis'
  environment: environmentName
}

@description('TdpGis.Api image, e.g. docker.io/myuser/tdpgis-api:1.0.0')
param apiImage string

@description('TdpGis.Endpoints (admin) image, e.g. docker.io/myuser/tdpgis-endpoints:1.0.0')
param endpointsImage string

@description('Container registry host. Use docker.io for Docker Hub, or your ACR login server. Empty = public pull.')
param registryServer string = 'docker.io'

@description('Registry username (Docker Hub or ACR). Empty = public pull. Password must already exist in Key Vault as registry-password.')
param registryUsername string = ''

@description('User-assigned identity name from bootstrap. Empty uses id-<prefix>-containerapps-<env>.')
param appsIdentityName string = ''

@description('Key Vault name from the bootstrap deploy. Empty uses the same generated name as bootstrap.')
param keyVaultName string = ''

@description('Entra instance base URL.')
#disable-next-line no-hardcoded-env-urls
param azureAdInstance string = 'https://login.microsoftonline.com/'

param apiMinReplicas int = 1
param apiMaxReplicas int = 3
param endpointsMinReplicas int = 1
param endpointsMaxReplicas int = 3

var resourceSuffix = '${namePrefix}-${environmentName}'
var logAnalyticsName = 'log-${resourceSuffix}'
var appInsightsName = 'appi-${resourceSuffix}'
var environmentResourceName = 'cae-${resourceSuffix}'
var apiAppName = 'ca-${namePrefix}-api-${environmentName}'
var endpointsAppName = 'ca-${namePrefix}-admin-${environmentName}'
var resolvedIdentityName = empty(appsIdentityName) ? 'id-${namePrefix}-containerapps-${environmentName}' : appsIdentityName
var generatedKeyVaultName = take(
  'kv-${take(namePrefix, 6)}-${take(environmentName, 3)}-${uniqueString(resourceGroup().id)}',
  24
)
var resolvedKeyVaultName = empty(keyVaultName) ? generatedKeyVaultName : keyVaultName
var hasRegistry = !empty(registryServer) && !empty(registryUsername)

resource appsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: resolvedIdentityName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: resolvedKeyVaultName
}

var kvUri = keyVault.properties.vaultUri

var databaseConnectionSecretRef = {
  name: 'database-connection-string'
  keyVaultUrl: '${kvUri}secrets/database-connection-string'
}

var registryPasswordSecretRef = {
  name: 'registry-password'
  keyVaultUrl: '${kvUri}secrets/registry-password'
}

var apiEntraSecretRefs = [
  {
    name: 'api-azuread-tenant-id'
    keyVaultUrl: '${kvUri}secrets/api-azuread-tenant-id'
  }
  {
    name: 'api-azuread-client-id'
    keyVaultUrl: '${kvUri}secrets/api-azuread-client-id'
  }
  {
    name: 'api-azuread-audience'
    keyVaultUrl: '${kvUri}secrets/api-azuread-audience'
  }
  {
    name: 'api-azuread-api-access-app-role'
    keyVaultUrl: '${kvUri}secrets/api-azuread-api-access-app-role'
  }
]

var endpointsEntraSecretRefs = [
  {
    name: 'endpoints-azuread-client-secret'
    keyVaultUrl: '${kvUri}secrets/endpoints-azuread-client-secret'
  }
  {
    name: 'endpoints-azuread-tenant-id'
    keyVaultUrl: '${kvUri}secrets/endpoints-azuread-tenant-id'
  }
  {
    name: 'endpoints-azuread-client-id'
    keyVaultUrl: '${kvUri}secrets/endpoints-azuread-client-id'
  }
  {
    name: 'endpoints-azuread-callback-path'
    keyVaultUrl: '${kvUri}secrets/endpoints-azuread-callback-path'
  }
  {
    name: 'endpoints-azuread-admin-app-role'
    keyVaultUrl: '${kvUri}secrets/endpoints-azuread-admin-app-role'
  }
  {
    name: 'endpoints-azuread-viewer-app-role'
    keyVaultUrl: '${kvUri}secrets/endpoints-azuread-viewer-app-role'
  }
]

var sharedKeyVaultSecrets = concat(
  [
    databaseConnectionSecretRef
  ],
  hasRegistry ? [registryPasswordSecretRef] : []
)

module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'logAnalytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

module applicationInsights 'modules/applicationInsights.bicep' = {
  name: 'applicationInsights'
  params: {
    name: appInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalytics.outputs.id
  }
}

module containerAppsEnvironment 'modules/containerAppsEnvironment.bicep' = {
  name: 'containerAppsEnvironment'
  params: {
    name: environmentResourceName
    location: location
    tags: tags
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsSharedKey: logAnalytics.outputs.primarySharedKey
  }
}

module apiApp 'modules/containerApp.bicep' = {
  name: 'apiContainerApp'
  params: {
    name: apiAppName
    location: location
    tags: union(tags, { app: 'TdpGis.Api' })
    environmentId: containerAppsEnvironment.outputs.id
    image: apiImage
    minReplicas: apiMinReplicas
    maxReplicas: apiMaxReplicas
    registryServer: registryServer
    registryUsername: registryUsername
    userAssignedIdentityId: appsIdentity.id
    keyVaultSecrets: concat(sharedKeyVaultSecrets, apiEntraSecretRefs)
    secretEnvVars: [
      {
        name: 'ConnectionStrings__Database'
        secretRef: 'database-connection-string'
      }
      {
        name: 'AzureAd__TenantId'
        secretRef: 'api-azuread-tenant-id'
      }
      {
        name: 'AzureAd__ClientId'
        secretRef: 'api-azuread-client-id'
      }
      {
        name: 'AzureAd__Audience'
        secretRef: 'api-azuread-audience'
      }
      {
        name: 'AzureAd__ApiAccessAppRole'
        secretRef: 'api-azuread-api-access-app-role'
      }
    ]
    envVars: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'AzureAd__Instance'
        value: azureAdInstance
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsights.outputs.connectionString
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

module endpointsApp 'modules/containerApp.bicep' = {
  name: 'endpointsContainerApp'
  params: {
    name: endpointsAppName
    location: location
    tags: union(tags, { app: 'TdpGis.Endpoints' })
    environmentId: containerAppsEnvironment.outputs.id
    image: endpointsImage
    minReplicas: endpointsMinReplicas
    maxReplicas: endpointsMaxReplicas
    registryServer: registryServer
    registryUsername: registryUsername
    userAssignedIdentityId: appsIdentity.id
    keyVaultSecrets: concat(sharedKeyVaultSecrets, endpointsEntraSecretRefs)
    secretEnvVars: [
      {
        name: 'ConnectionStrings__Database'
        secretRef: 'database-connection-string'
      }
      {
        name: 'AzureAd__ClientSecret'
        secretRef: 'endpoints-azuread-client-secret'
      }
      {
        name: 'AzureAd__TenantId'
        secretRef: 'endpoints-azuread-tenant-id'
      }
      {
        name: 'AzureAd__ClientId'
        secretRef: 'endpoints-azuread-client-id'
      }
      {
        name: 'AzureAd__CallbackPath'
        secretRef: 'endpoints-azuread-callback-path'
      }
      {
        name: 'AzureAd__AdminAppRole'
        secretRef: 'endpoints-azuread-admin-app-role'
      }
      {
        name: 'AzureAd__ViewerAppRole'
        secretRef: 'endpoints-azuread-viewer-app-role'
      }
    ]
    envVars: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'AzureAd__Instance'
        value: azureAdInstance
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsights.outputs.connectionString
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

output keyVaultNameOut string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output appsIdentityNameOut string = appsIdentity.name
output containerAppsEnvironmentName string = containerAppsEnvironment.outputs.name
output containerAppsEnvironmentDefaultDomain string = containerAppsEnvironment.outputs.defaultDomain
output applicationInsightsName string = applicationInsights.outputs.name
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString
output apiFqdn string = apiApp.outputs.fqdn
output apiUrl string = 'https://${apiApp.outputs.fqdn}'
output endpointsFqdn string = endpointsApp.outputs.fqdn
output endpointsUrl string = 'https://${endpointsApp.outputs.fqdn}'
output apiAppNameOut string = apiApp.outputs.name
output endpointsAppNameOut string = endpointsApp.outputs.name
