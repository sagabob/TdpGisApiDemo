# TDP GIS API Backend

ASP.NET Core backend for the TDP GIS demo: a query API (`TdpGis.Api`), an admin UI (`TdpGis.Endpoints`), and shared domain/application/infrastructure layers.

## Overview

- **TdpGis.Api** — REST GIS query API (FastEndpoints + Swagger). Callers authenticate with Microsoft Entra ID and authorize per workspace with an opaque access token.
- **TdpGis.Endpoints** — Admin MVC UI (OpenID Connect cookie). Configure data sources, GIS entities, workspaces, and workspace tokens.
- **Shared app database** — PostgreSQL via EF Core (`ConnectionStrings:Database`). Holds configuration metadata (sources, entities, workspaces, tokens).
- **GIS data sources** — Configured in admin; GIS queries run against **MongoDB**, **PostgreSQL**, or **SQL Server** at request time.

## Architecture

The GIS query API follows **Clean Architecture** (ports and adapters):

```text
Endpoint (Api) → UseCase (Application) → Ports (interfaces) → Adapters (Infrastructure)
```

| Project | Role |
|---------|------|
| `TdpGis.Api` | HTTP adapters (FastEndpoints), Entra JWT policy, HTTP status mapping |
| `TdpGis.Endpoints` | Admin UI, Entra OIDC cookie, roles `Gis.Admin` / `Gis.Viewer` |
| `TdpGis.Application` | Use cases + ports; shared guards and failure kinds |
| `TdpGis.AdminApplication` | Admin abstractions (metadata probes, etc.) |
| `TdpGis.Infrastructure` | Adapters: EF, Mongo/SQL; DI registers ports **and** use cases |
| `TdpGis.Domain` | Entities (`GisConnection`, `DataSourceSetting`, workspaces, tokens) |

### Use cases (Application)

| Use case | Responsibility |
|----------|----------------|
| `SearchGisEntityUseCase` | Phrase query by `QueryField` |
| `GetGisWorkspaceEntitiesUseCase` | List entity definitions for a workspace |

Shared workspace-token checks live in `WorkspaceAccessGuard` (not in endpoints).

### Failures vs HTTP

- Application returns **`GisQueryFailureKind`** (e.g. `MissingWorkspaceAccessToken`, `EntityNotFound`) — **no raw `400`/`401`/`404` in Application**.
- Api maps kinds to status codes in **`GisQueryHttp`**.
- `GisWorkspaceAccess` only **reads** the `X-Access-Token` header; validation is in the use case / guard.

`GisDataService` routes GIS data access by `DataSource.DatabaseType` to Mongo or relational SQL implementations.

### Cursor rules and skills (same style on new APIs)

This pattern is captured so agents reuse it:

| Location | Purpose |
|----------|---------|
| `_Backend/.cursor/rules/clean-architecture-api.mdc` | Project rule when editing Api / Application / Infrastructure |
| Repo `.cursor/rules/tdpgis-clean-architecture-api.mdc` | Same conventions at repo level |
| Personal skill `dotnet-clean-architecture-api` (`~/.cursor/skills/...`) | Scaffold or implement **new** .NET APIs the same way |

When starting a new API project, ask the agent to follow the **dotnet-clean-architecture-api** skill (or “Endpoint → UseCase → Ports → Adapters like TdpGis”).

## Technology Stack

- .NET 10
- FastEndpoints 8.x + NSwag Swagger
- Microsoft Entra ID (`Microsoft.Identity.Web`)
- EF Core + PostgreSQL (app/config DB)
- MongoDB, PostgreSQL, SQL Server (GIS entity data)
- xunit.v3 + Microsoft.Testing.Platform (`global.json`)

## Prerequisites

- .NET 10 SDK
- PostgreSQL connection for the **app** database
- Entra app registrations (API resource + optional admin web app + caller)
- GIS source credentials as needed (Mongo / Postgres / SQL Server)

## Getting Started

```bash
cd TdpGisApiDemo/_Backend
dotnet restore
dotnet ef database update -p TdpGis.Infrastructure -s TdpGis.Api
dotnet run --project TdpGis.Api
dotnet run --project TdpGis.Endpoints
```

### Configuration (`TdpGis.Api`)

`appsettings.Development.json` (or User Secrets):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<directory-tenant-id>",
    "ClientId": "<api-app-client-id>",
    "Audience": "api://<api-app-client-id>",
    "ApiAccessAppRole": "TdpGisApi.Access"
  },
  "ConnectionStrings": {
    "Database": "Host=...;Port=...;Database=...;Username=...;Password=...;Include Error Detail=true"
  }
}
```

Prefer secrets over committing passwords:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "Host=...;..." --project TdpGis.Api
```

### Local URLs (launch profiles)

| App | HTTPS | HTTP |
|-----|-------|------|
| `TdpGis.Api` | `https://localhost:7255` | `http://localhost:5236` |
| `TdpGis.Endpoints` | `https://localhost:7036` | `http://localhost:5291` |

- Swagger: `https://localhost:7255/swagger`
- Health: `GET /health` (live), `GET /health/ready` (DB) — **anonymous**

---

## Authentication and authorization

Security is split by product surface. **Do not mix** the admin web app registration with the API resource registration unless you intentionally design one app for both.

### Mental model

```text
Caller (frontend BFF / Postman / daemon)
  │
  ├─ Authorization: Bearer <Entra access token>
  │     → proves identity + API app role (authentication + Entra authorization)
  │
  └─ X-Access-Token: <workspace opaque token>
        → proves access to a specific GIS workspace (application authorization)
              │
              ▼
         TdpGis.Api GIS endpoints
```

Bearer is **only** for Entra. Workspace tokens are **never** sent as Bearer.

---

### TdpGis.Api — two layers

#### Layer 1: Microsoft Entra ID (JWT Bearer)

**What it is**

- Header: `Authorization: Bearer <access_token>`
- Validated by `AddMicrosoftIdentityWebApi` against the `AzureAd` config section
- Checks issuer, signature, lifetime, and audience
- Accepted audiences: `AzureAd:Audience`, raw `AzureAd:ClientId`, and `api://{ClientId}`

**Authorization (app role)**

- Named policy `TdpGisApiAccess` is applied to **all FastEndpoints** (not as `FallbackPolicy`, so Swagger stays anonymous)
- Requires an authenticated user
- Requires claim `roles` to contain `AzureAd:ApiAccessAppRole` (default **`TdpGisApi.Access`**)
- Role matching is implemented in `EntraAppRoleClaims` (handles Entra `roles` claim types)

**Who must have the role**

| Caller style | How the role appears |
|--------------|----------------------|
| User (delegated) | User/group assigned `TdpGisApi.Access` on the API enterprise app |
| App-only (client credentials) | Application permission / app role assigned to the **client** service principal |

**Entra setup (API resource app)**

1. App registration for **TdpGis.Api** → copy Application (client) ID into `AzureAd:ClientId`
2. **Expose an API** → Application ID URI `api://<ClientId>` → `AzureAd:Audience`
3. Optional delegated scope (e.g. `access_as_user`) for **user** tokens:  
   `api://<ClientId>/access_as_user`
4. **App roles** → value **`TdpGisApi.Access`** (Users/Groups and/or Applications)
5. Assign the role to users/groups or to the calling app
6. Caller app: API permission (delegated scope or application role) + admin consent

**Getting a token**

- **User (delegated):** authorize + token with scope  
  `api://<ApiClientId>/access_as_user`  
  (or your exposed scope name)
- **App-only:** client credentials with scope  
  `api://<ApiClientId>/.default`

Decode at [jwt.ms](https://jwt.ms): `aud` must be this API; `roles` should include `TdpGisApi.Access`.

**Typical HTTP results (Entra layer)**

| Status | Meaning |
|--------|---------|
| 401 | Missing/invalid/expired Bearer, wrong audience, bad signature |
| 403 | Authenticated but missing `TdpGisApi.Access` |

#### Layer 2: Workspace access token

**What it is**

- Header: **`X-Access-Token`**
- Opaque string created in **TdpGis.Endpoints** (Workspace & token UI)
- Validated in Application (`WorkspaceAccessGuard`) against the app DB (workspace id, active, not expired)

**Why it exists**

Entra proves “this caller may use the API.” The workspace token proves “this caller may query **this** workspace’s entities.”

**Typical HTTP results (workspace layer)**

| Status | Meaning |
|--------|---------|
| 400 | Missing `X-Access-Token` or invalid workspace id |
| 401 | Token invalid, inactive, expired, or wrong workspace |
| 404 | Entity not in workspace (phrase query) — auth already succeeded |

#### GIS API routes (both headers required)

```http
GET /api/gis-workspace-entities/{workspaceId}
Authorization: Bearer <entra-access-token>
X-Access-Token: <workspace-token>

# Phrase query (by configured QueryField) — one of the GIS query operations
GET /api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}
Authorization: Bearer <entra-access-token>
X-Access-Token: <workspace-token>
```

Responses use typed DTOs (`List<GisConnectionDto>`, `SearchGisEntityResponse`, errors as `ApiMessageResponse`).

Additional query types (for example **spatial search**) are expected to follow the same auth headers and workspace/entity routing, with their own routes and request shapes.

#### Pipeline order (`TdpGis.Api`)

```text
UseAuthentication()   → validate Entra JWT (or mock in tests)
UseAuthorization()    → TdpGisApiAccess policy (role)
FastEndpoints handler → GisWorkspaceAccess (X-Access-Token)
                      → load entity config → GisDataService (list / query)
```

---

### TdpGis.Endpoints — admin UI (separate Entra app)

Admin uses **OpenID Connect cookie** sign-in (not the API JWT policy).

| Setting | Purpose |
|---------|---------|
| `AzureAd:ClientId` / `ClientSecret` | Web app registration |
| `AzureAd:CallbackPath` | Usually `/signin-oidc` |
| `AzureAd:AdminAppRole` | Default `Gis.Admin` — can manage configuration |
| `AzureAd:ViewerAppRole` | Default `Gis.Viewer` — read-oriented access |

Create app roles **`Gis.Admin`** and **`Gis.Viewer`** on the **admin** app registration and assign users. This is independent of **`TdpGisApi.Access`** on the API app.

---

### Frontend / BFF callers (how tokens are used)

Two common patterns:

1. **App-only to API** — BFF uses client credentials; sets Bearer from an app token. User login is for the site/session only. (This repo’s `_Frontend` prefers `gis_api_access_token` for upstream GIS calls.)
2. **Delegated `access_as_user`** — user signs in with scope for the API; BFF forwards the user’s access token as Bearer. User must have `TdpGisApi.Access`.

In both cases the BFF (or client) still sends **`X-Access-Token`** for the workspace (from secure server config or admin-issued token).

---

### Testing auth (API integration tests)

Production Entra validation is **not** used in `TdpGis.Api.Tests`. `TdpGisApiWebApplicationFactory` sets:

| Setting | Effect |
|---------|--------|
| `IntegrationTests:UseMockJwt=true` | Replaces Entra with `IntegrationTestJwtAuthenticationHandler` (parse JWT claims only; no signature/lifetime/audience) |
| `IntegrationTests:SkipApiAccessRole=true` | Policy requires authenticated user but skips `TdpGisApi.Access` |

Tests still send `Authorization: Bearer` (fake token) and exercise **`X-Access-Token`** / workspace validation with mocked `IGisConfigurationService`.

**Never** enable those flags in production configuration.

---

## GIS data sources and queries

Data sources and entities are configured in the admin UI, stored in the app Postgres DB, and used at query time by `TdpGis.Api`.

Supported source types: **MongoDB**, **PostgreSQL**, **SQL Server**.

### Query operations

Phrase search by `QueryField` is the query operation implemented today. More operations (such as **spatial search**) will be added using the same workspace/entity configuration and auth model.

| Operation (current) | Route | Behavior |
|---------------------|-------|----------|
| List workspace entities | `GET /api/gis-workspace-entities/{workspaceId}` | Returns entity definitions (no source round-trip for row data) |
| Phrase query | `GET .../entity/{entityId}/search/{searchedPhrase}` | Filters source rows where configured `QueryField` contains the phrase |

**Phrase query by source type**

| `SourceType` | Phrase filter |
|--------------|---------------|
| `Mongodb` | Case-insensitive regex on `QueryField` |
| `Postgres` | `ILIKE` on `QueryField`; mapped columns projected (geometry as text) |
| `SqlServer` | `LIKE` on `QueryField`; mapped columns projected |

Phrase queries project configured **property mappings** (no per-request schema probe).

---

## Project structure (API-focused)

```
TdpGis.Api/
├── Authentication/          # EntraAppRoleClaims, IntegrationTestJwtAuthenticationHandler
├── GisQuery/
│   ├── Endpoints/           # Thin HTTP adapters → use cases
│   ├── Helpers/             # GisWorkspaceAccess (header), GisQueryHttp (kind → status)
│   └── Messages/            # Requests/responses (typed DTOs)
└── Program.cs               # Auth pipeline, policies, Swagger schemes

TdpGis.Application/
├── Abstractions/            # Ports (IGisConfigurationService, IGisDataService, …)
├── Common/                  # WorkspaceAccessGuard, GisQueryFailureKind
├── UseCases/
│   ├── SearchGisEntity/
│   └── GetGisWorkspaceEntities/
└── AppModels/               # Public DTOs (no connection strings on list)

TdpGis.Infrastructure/
├── GisDataService.cs        # Routes Mongo vs SQL data access
├── Mongo/                   # Mongo adapters
├── Sql/                     # Postgres/SQL Server adapters
├── Persistence/             # EF app DB + GisConfigurationService
└── DependencyInjection/     # Registers ports + use cases
```

## Development

### Tests

This repo uses **Microsoft.Testing.Platform** (see `global.json`). From `_Backend`:

```bash
dotnet test --project tests/TdpGis.Api.Tests/TdpGis.Api.Tests.csproj
dotnet test --project tests/TdpGis.Infrastructure.Tests/TdpGis.Infrastructure.Tests.csproj
dotnet test --project tests/TdpGis.Application.Tests/TdpGis.Application.Tests.csproj
```

### Build / Docker

```bash
dotnet build
dotnet publish -c Release -o ./publish

docker build -f TdpGis.Api/Dockerfile -t tdp-gis-api:latest .
docker run -p 8080:8080 \
  -e AzureAd__TenantId=<tenant-id> \
  -e AzureAd__ClientId=<client-id> \
  -e AzureAd__Audience=api://<client-id> \
  -e ConnectionStrings__Database=<postgres-connection-string> \
  tdp-gis-api:latest
```

## Troubleshooting

### Entra / API 401

- Token expired (`IDX10223`) — get a new access token
- Wrong audience — use an **access token for this API**, not an ID token or Graph token; confirm `aud` on jwt.ms
- Missing Bearer header

### Entra / API 403

- User or app not assigned role **`TdpGisApi.Access`**
- Role **Value** in Entra must match `AzureAd:ApiAccessAppRole` exactly

### Workspace 400 / 401

- Missing `X-Access-Token`
- Token revoked, expired, or for a different workspace

### GIS query errors

- Postgres geometry mapped without text projection — phrase queries project Postgres columns as `::text` for reader compatibility
- Ensure property mappings and `QueryField` match the source table/collection for phrase queries

### Database

- App DB must be reachable PostgreSQL (`ConnectionStrings:Database`)
- Run EF migrations against that database

## Additional resources

- [Microsoft Entra ID](https://learn.microsoft.com/entra/identity/)
- [Microsoft identity platform access tokens](https://learn.microsoft.com/entra/identity-platform/access-tokens)
- [FastEndpoints](https://fast-endpoints.com/)
- [EF Core](https://learn.microsoft.com/ef/core/)
- [dotnet test + Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test)
