using './main.bicep'

param location = 'australiaeast'
param namePrefix = 'tdpgis'
param environmentName = 'demo'

// Must match bootstrap.bicepparam.
param appsIdentityName = 'id-tdpgis-containerapps-demo'
param keyVaultName = 'kv-tdpgis-demo-01'

// Pin image tags for demo; do not use :latest.
param apiImage = 'docker.io/<dockerhub-user>/tdpgis-api:<version>'
param endpointsImage = 'docker.io/<dockerhub-user>/tdpgis-endpoints:<version>'

param registryServer = 'docker.io'
param registryUsername = '<dockerhub-user>'

param apiMinReplicas = 1
param apiMaxReplicas = 3
param endpointsMinReplicas = 1
param endpointsMaxReplicas = 3
