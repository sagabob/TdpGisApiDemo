@description('Reusable Azure Container App for a TdpGis Docker image (port 8080).')
param name string
param location string = resourceGroup().location
param tags object = {}
param environmentId string

@description('Full image reference, e.g. docker.io/myuser/tdpgis-api:latest')
param image string

@description('Target container port (matches ASPNETCORE_URLS in Dockerfiles).')
param targetPort int = 8080

@description('Expose the app on the public Container Apps ingress.')
param externalIngress bool = true

@description('Minimum replicas (0 allows scale-to-zero).')
param minReplicas int = 0

@description('Maximum replicas.')
param maxReplicas int = 3

@description('CPU cores allocated to the container (e.g. 0.25, 0.5, 1.0).')
param cpu string = '0.5'

@description('Memory allocated to the container (e.g. 1Gi).')
param memory string = '1Gi'

@description('Non-secret environment variables.')
param envVars array = []

@description('Inline secret definitions (name + value). Prefer keyVaultSecrets.')
param secrets array = []

@description('Key Vault secret refs (name + keyVaultUrl). Requires userAssignedIdentityId.')
param keyVaultSecrets array = []

@description('Environment variables that reference secrets (name + secretRef).')
param secretEnvVars array = []

@description('User-assigned identity used to resolve Key Vault secret references.')
param userAssignedIdentityId string = ''

@description('Optional container registry (Docker Hub / ACR). Leave empty for public images.')
param registryServer string = ''
param registryUsername string = ''
@secure()
param registryPassword string = ''

@description('Liveness probe path.')
param livenessPath string = '/health'

@description('Readiness probe path.')
param readinessPath string = '/health/ready'

var hasRegistry = !empty(registryServer) && !empty(registryUsername)
var hasUserAssignedIdentity = !empty(userAssignedIdentityId)
var registryInlineSecrets = hasRegistry && !hasUserAssignedIdentity ? [
  {
    name: 'registry-password'
    value: registryPassword
  }
] : []

var mappedKeyVaultSecrets = [
  for s in keyVaultSecrets: {
    name: s.name
    keyVaultUrl: s.keyVaultUrl
    identity: userAssignedIdentityId
  }
]

var allSecrets = concat(mappedKeyVaultSecrets, secrets, registryInlineSecrets)

var registryConfig = hasRegistry ? [
  {
    server: registryServer
    username: registryUsername
    passwordSecretRef: 'registry-password'
  }
] : []

var mappedEnv = [for e in envVars: {
  name: e.name
  value: e.value
}]

var mappedSecretEnv = [for e in secretEnvVars: {
  name: e.name
  secretRef: e.secretRef
}]

resource containerApp 'Microsoft.App/containerApps@2025-01-01' = {
  name: name
  location: location
  tags: tags
  identity: hasUserAssignedIdentity
    ? {
        type: 'UserAssigned'
        userAssignedIdentities: {
          '${userAssignedIdentityId}': {}
        }
      }
    : {
        type: 'None'
      }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: externalIngress
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      secrets: allSecrets
      registries: registryConfig
    }
    template: {
      containers: [
        {
          name: name
          image: image
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: concat(mappedEnv, mappedSecretEnv)
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: livenessPath
                port: targetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 15
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: readinessPath
                port: targetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output latestRevisionName string = containerApp.properties.latestRevisionName
