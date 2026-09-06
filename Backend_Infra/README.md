# Deploy TdpGis.Api + TdpGis.Endpoints to Azure Container Apps

## What this creates

| Resource | Created by | Purpose |
|----------|------------|---------|
| **Key Vault** | `bootstrap.bicep` | Store of record for secrets and the DB connection |
| User-assigned identity | `bootstrap.bicep` | Container Apps read Key Vault (RBAC) |
| Log Analytics workspace | `main.bicep` | Container Apps logs |
| Container Apps Environment | `main.bicep` | Shared hosting environment |
| Container App `ca-*-api-*` | `main.bicep` | `TdpGis.Api` |
| Container App `ca-*-admin-*` | `main.bicep` | `TdpGis.Endpoints` |

Bicep never writes secret *values*. You create the vault first, set secrets out of band, then deploy the apps (they only reference secret names).

| Secret name | Used by | App setting |
|-------------|---------|-------------|
| `database-connection-string` | Api + Endpoints | `ConnectionStrings__Database` |
| `api-azuread-tenant-id` | Api | `AzureAd__TenantId` |
| `api-azuread-client-id` | Api | `AzureAd__ClientId` |
| `api-azuread-audience` | Api | `AzureAd__Audience` |
| `api-azuread-api-access-app-role` | Api | `AzureAd__ApiAccessAppRole` |
| `endpoints-azuread-tenant-id` | Endpoints | `AzureAd__TenantId` |
| `endpoints-azuread-client-id` | Endpoints | `AzureAd__ClientId` |
| `endpoints-azuread-client-secret` | Endpoints | `AzureAd__ClientSecret` |
| `endpoints-azuread-callback-path` | Endpoints | `AzureAd__CallbackPath` |
| `endpoints-azuread-admin-app-role` | Endpoints | `AzureAd__AdminAppRole` |
| `endpoints-azuread-viewer-app-role` | Endpoints | `AzureAd__ViewerAppRole` |
| `registry-password` | Api + Endpoints (if registry auth) | image pull |

## Demo first deploy

Use `scripts/Deploy-Demo-Infra.ps1`. It creates the resource group, deploys Key Vault, loads secrets from a local `.env` (or prompts), then deploys the apps. Bicep still never receives secret values.

### 0. Before you run

- Azure CLI
- Contributor (or equivalent) on the subscription / resource group
- Published demo images (pin a version tag, not `:latest`) in `main.bicepparam`
- The same `keyVaultName` and `environmentName` (`demo`) in `bootstrap.bicepparam` and `main.bicepparam`
- Reachable demo Postgres
- Entra app registrations (API + admin web)
- A local `Backend_Infra/.env` (copy from `.env.example`; this file is gitignored)

### 1. Fill `.env`

```powershell
copy .env.example .env
```

Edit `.env` with real values. Required keys:

```
TDPGIS_DATABASE_CONNECTION_STRING=
TDPGIS_API_TENANT_ID=
TDPGIS_API_CLIENT_ID=
TDPGIS_ENDPOINTS_CLIENT_ID=
TDPGIS_ENDPOINTS_CLIENT_SECRET=
```

Add `TDPGIS_REGISTRY_PASSWORD` if `registryUsername` is set in `main.bicepparam`.

### 2. Run the script

From `Backend_Infra`:

```powershell
.\scripts\Deploy-Demo-Infra.ps1 -SubscriptionId '<subscription-id>'
```

That is `-Phase All`: bootstrap → secrets from `.env` → apps.

Or one phase at a time:

```powershell
.\scripts\Deploy-Demo-Infra.ps1 -Phase Bootstrap -SubscriptionId '<subscription-id>'
.\scripts\Deploy-Demo-Infra.ps1 -Phase Secrets
.\scripts\Deploy-Demo-Infra.ps1 -Phase Apps
```

The script:

- Logs in if needed and selects the subscription
- Creates `rg-tdpgis-demo` if missing
- Resolves your Entra object ID as Key Vault Secrets Officer
- Waits for Key Vault RBAC before writing secrets
- Loads `Backend_Infra/.env` (or `-EnvFile`) and writes those values into Key Vault
- Prompts only for keys that are missing from `.env`
- Reuses the API tenant ID for Endpoints unless you override it
- Defaults audience to `api://<api-client-id>`
- Skips `registry-password` when `registryUsername` is empty
- Runs `what-if` before the app deploy (use `-SkipWhatIf` to skip)
- Prints `apiUrl`, `endpointsUrl`, and the Entra redirect URI

### 3. After the apps are up

Add the printed redirect URI to the admin app registration:

`https://<endpoints-fqdn>/signin-oidc`

Then hit `apiUrl` (`/swagger`) and `endpointsUrl`.

### Later changes

| Change | What to do |
|--------|------------|
| Rotate DB password or client secret | `.\scripts\Deploy-Demo-Infra.ps1 -Phase Secrets`, then restart the Container App revision |
| New image version | Update `main.bicepparam` and run `-Phase Apps` |
| Vault / identity / RBAC | `-Phase Bootstrap` (does not touch secret values) |

## Notes

- Images already set `ASPNETCORE_URLS=http://0.0.0.0:8080`; ingress terminates TLS.
- Endpoints uses forwarded headers for OIDC behind the Container Apps proxy.
- Demo defaults to `minReplicas=1` (always on).
- Secret URIs are unversioned; a new Key Vault version is picked up when the revision restarts.
- `enablePurgeProtection=true` on bootstrap cannot be turned off later.
- Public images: set `registryUsername` to `''` and skip `registry-password`.
