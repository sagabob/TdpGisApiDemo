using './main.bicep'

param location = 'australiaeast'
param namePrefix = 'tdpgis'
param environmentName = 'demo'

// Must match bootstrap.bicepparam.
param appsIdentityName = 'id-tdpgis-containerapps-demo'
param keyVaultName = 'kv-tdpgis-demo-01'

// Pin image tags for demo; do not use :latest.
param apiImage = 'docker.io/bobpham/tdpgis-api:latest'
param endpointsImage = 'docker.io/bobpham/tdpgis-admin-ui:latest'

param registryServer = 'docker.io'
// Public Docker Hub images: leave empty. Private images: set the username and
// put TDPGIS_REGISTRY_PASSWORD in .env, then run -Phase Secrets before -Phase Apps.
param registryUsername = ''

param apiMinReplicas = 1
param apiMaxReplicas = 3
param endpointsMinReplicas = 1
param endpointsMaxReplicas = 3
