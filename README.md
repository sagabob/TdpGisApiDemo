# TdpGisApiDemo

The **end goal** of this repository is the **GIS map frontend** in **`_Frontend/`**: a browser app that queries **`TdpGis.Api`** and displays workspace GIS features on a Mapbox map.

The **backend** in **`_Backend/`** powers that API and an **MVC admin** (`TdpGis.Endpoints`) where you configure MongoDB connections, GIS entities, workspaces, and access tokens. Admins sign in with **Microsoft Entra ID** (OpenID Connect); **app roles** control who can view or edit configuration. The **GIS REST API** (`TdpGis.Api`) requires both **Entra JWT Bearer** and **workspace access token** (`X-Access-Token`). Relational metadata is stored in **PostgreSQL** (Entity Framework Core); GIS payloads are read from **MongoDB** using saved configuration.

## Live demos

| App | URL |
|-----|-----|
| **GIS map frontend** (`_Frontend`) | [https://tdp-gis-api-demo.vercel.app/](https://tdp-gis-api-demo.vercel.app/) |
| **REST API** (`TdpGis.Api`; Swagger at `/swagger`) | [https://urchin-app-f57y9.ondigitalocean.app/](https://urchin-app-f57y9.ondigitalocean.app/) |
| **Admin UI** (`TdpGis.Endpoints`) | [https://seal-app-q3vt5.ondigitalocean.app/](https://seal-app-q3vt5.ondigitalocean.app/) |

## GIS map frontend (`_Frontend/`)

This is the browser client that consumes **`TdpGis.Api`** and shows search results on the map. It uses **Vite + React** (TypeScript, **Tailwind CSS**, **Mapbox** / `react-map-gl`, **Axios**). The browser only calls same-origin **`/api/*`** routes; **Vercel serverless handlers** under `_Frontend/api/` (and the Vite dev proxy locally) forward to the real API and attach **`X-Access-Token`** and **`Authorization: Bearer`** server-side so tokens are not embedded in the client bundle.

**Try the demo**

Go to the frontend demo link, type a phrase such as 'park' or 'garden' to see the results. The demo shows capability to search place names and addresses

**Runtime flow**

- `useWorkspaceEntities` loads workspace entities from `/api/gis/workspace-entities`.
- `useWorkspaceGeoSearch` searches selected entities via `/api/gis/workspace-entity-search?entityId=...&q=...&workspaceId=...`.
- Responses are normalized and rendered as map markers and a selection overlay in `GisMap`.

**Run locally**

```bash
cd _Frontend
npm install
npm run dev
```

More detail: `_Frontend/README.md`.

---

## Backend (`_Backend/`)

The backend exposes two hosts: a **FastEndpoints REST API** (`TdpGis.Api`) for GIS queries (used by the frontend) and an **MVC admin** (`TdpGis.Endpoints`) for configuration, with **Microsoft Entra ID** sign-in and **role-based** access to the configuration UI.

### Architecture (Clean Architecture)

The backend follows **Clean Architecture**: **dependency direction points inward**. Outer layers depend on inner ones; the **domain** and **application** layers stay free of databases, HTTP, or UI frameworks.

**`TdpGis.Application`** and **`TdpGis.AdminApplication`** are **sibling** use case assemblies: they do **not** depend on each other. Both depend only on **`TdpGis.Domain`**. The first models **end-user** use cases (GIS API consumers); the second models **admin** use cases (configuration and metadata management). Each exposes **ports** (interfaces) that **Infrastructure** implements.

| Layer | Project | Role |
|-------|---------|------|
| **Domain** | `TdpGis.Domain` | Entities and core types. **No** references to other projects in the solution. |
| **Application (end-user)** | `TdpGis.Application` | **Use cases** for **end users**: ports such as `IGisConfigurationService`, `IGisDataService`, plus DTOs. Depends only on **Domain**. |
| **Application (admin)** | `TdpGis.AdminApplication` | **Use cases** for **admins**: `IGisAdminAppService`, configuration **repository** contract (`IGisConfigurationRepository`), and related types. Depends only on **Domain**. |
| **Infrastructure** | `TdpGis.Infrastructure` | **Adapters**: EF Core (PostgreSQL), MongoDB drivers, repository and service implementations. Depends on **Application**, **AdminApplication**, and **Domain**; registers implementations in **`AddInfrastructure`**. |
| **Presentation / composition** | `TdpGis.Endpoints`, `TdpGis.Api` | **Hosts**: MVC or FastEndpoints, HTTP concerns. **`TdpGis.Endpoints`**: Entra ID + cookies. **`TdpGis.Api`**: workspace access tokens only. Both reference **Infrastructure** to compose the graph at startup. |

**`TdpGis.Api`** uses the **end-user** application layer; **`TdpGis.Endpoints`** uses the **admin** application layer. Both share **Infrastructure** and **Domain**; only **hosting** and **transport** differ (REST vs Razor).

### Solution layout

All backend projects live under `_Backend/`:

| Project | Description |
|--------|-------------|
| **TdpGis.Endpoints** | Runnable **MVC** app: Razor views, static assets, **Entra ID (OIDC) + cookie session** for the configuration UI, **app-role** authorization. References **Domain**, **AdminApplication**, and **Infrastructure**. Entry point: `Program.cs`. |
| **TdpGis.Api** | Runnable **FastEndpoints** host: GIS query endpoints, **Swagger/OpenAPI**. References **Application** and **Infrastructure**. Entry point: `Program.cs`. |
| **TdpGis.Infrastructure** | EF Core **`GisAppDbContext`**, Fluent configurations, **migrations**, PostgreSQL access, **`GisConfigurationRepository`**, MongoDB helpers (`MongoClientCache`, `MongoMetadataProvider`, **`GisMongoDataService`**). Registers **Data Protection** key persistence into the same database for the MVC host (see `EndpointsDataProtectionExtensions`). |
| **TdpGis.AdminApplication** | **Admin** use cases and UI orchestration: **`IGisAdminAppService`** / **`GisAdminAppService`** for the configuration page. |
| **TdpGis.Application** | **End-user** use cases: ports (**`IGisConfigurationService`**, **`IGisDataService`**, etc.) and shared app models/DTOs. |
| **TdpGis.Domain** | Domain entities: data sources, GIS connections, property mappings, workspaces, access tokens. |

Open the solution:

```bash
dotnet build _Backend/TdpGisApiDemo.slnx
```

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL** for application metadata (EF Core)
- **MongoDB** (optional until you add Mongo connection strings and GIS entities that read collections)

### Configuration

#### PostgreSQL (EF Core)

Set **`ConnectionStrings:Database`** to a valid PostgreSQL connection string. This applies to both **`TdpGis.Endpoints`** and **`TdpGis.Api`** (each has its own `appsettings`; use `appsettings.Development.json` locally or **environment variables** / **user secrets** for secrets).

Base `appsettings.json` files may leave the connection string empty; ensure it is supplied at runtime (e.g. `ConnectionStrings__Database`).

**Cookie auth keys:** The admin app stores **ASP.NET Data Protection** keys in PostgreSQL (`PersistKeysToDbContext<GisAppDbContext>`) so authentication cookies remain valid across container restarts without a file volume. Apply EF migrations **before** relying on login in production or Docker; the migrations include the Data Protection keys table.

#### Microsoft Entra ID sign-in (`TdpGis.Endpoints` only)

The configuration UI uses **OpenID Connect** against **Microsoft Entra ID**. Set **`AzureAd`** in configuration (`Instance`, `TenantId`, `ClientId`, `ClientSecret`, `CallbackPath`, and app role value strings `AdminAppRole` / `ViewerAppRole`, which must match each app role’s **Value** in the app registration). Assign users or groups to those roles under **Enterprise applications** → your app → **Users and groups**.

**Docker / environment variables** use double underscores, e.g. `AzureAd__TenantId`, `AzureAd__ClientSecret`, `AzureAd__AdminAppRole`, `AzureAd__ViewerAppRole`.

See `TdpGis.Endpoints/appsettings.json` for placeholders.

### Database

Apply EF Core migrations to create or update the schema. Migrations live in **`TdpGis.Infrastructure`**. Example (from repo root):

```bash
dotnet ef database update --project _Backend/TdpGis.Infrastructure/TdpGis.Infrastructure.csproj --startup-project _Backend/TdpGis.Endpoints/TdpGis.Endpoints.csproj --context GisAppDbContext
```

You can use **`TdpGis.Api`** as the startup project instead if you prefer; it must be able to read the same **`ConnectionStrings:Database`** at design time.

> **Note:** `TdpGis.Infrastructure` references SQL Server and Npgsql packages; the app registers **PostgreSQL** via **`UseNpgsql`** in `AddInfrastructure`.

### Run the admin web app (`TdpGis.Endpoints`)

```bash
cd _Backend/TdpGis.Endpoints
dotnet run
```

Or open `_Backend/TdpGisApiDemo.slnx` in Visual Studio / Rider and start **TdpGis.Endpoints**.

Default URLs (see `Properties/launchSettings.json`): **https://localhost:7036** and **http://localhost:5291**.

**Health checks** (anonymous; no cookie required): **`GET /health`** (liveness) and **`GET /health/ready`** (readiness, includes a database check). In Development, HTTPS redirection is skipped for `/health*` so plain HTTP probes work.

Use **`/Account/Login`** (OIDC challenge to Microsoft), then open **`/Home/Configuration`**. Users need the **Gis Viewer** or **Gis Admin** app role (or they are sent to **`/Home/AccessDenied`**).

### Run the REST API (`TdpGis.Api`)

```bash
cd _Backend/TdpGis.Api
dotnet run
```

Default URLs (see `Properties/launchSettings.json`): **https://localhost:7255** and **http://localhost:5236**.

**CORS** is enabled with a **default policy** that allows any origin, method, and header (useful for browser clients such as the `_Frontend` dev server).

With the app running, open the **Swagger UI** (FastEndpoints + Swagger) at **`/swagger`** on that host.

### REST API (GIS query)

GIS endpoints require both:
- **Entra access token** in `Authorization: Bearer` (JWT policy + app role check), and
- **Workspace access token** in `X-Access-Token` (validated against workspace/token records).

In the frontend BFF, bearer is resolved from:
1. `gis_api_access_token` HTTP-only cookie (client credentials bootstrap),
2. `auth_access_token` HTTP-only cookie (signed-in user),
3. `REST_API_BEARER_TOKEN` / `PUBLIC_API_BEARER_TOKEN` env fallback.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/gis-workspace-entities/{workspaceId}` | List GIS entity definitions (DTOs) for the workspace. |
| GET | `/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}` | Search within a GIS entity’s collection (subject to ongoing implementation). |

### Admin UI routes (`TdpGis.Endpoints`)

| Route | Purpose |
|-------|---------|
| `/` (`Home/Index`) | Public landing (**`Project`** view). |
| `/Account/Login` | Starts Entra ID sign-in (OIDC challenge). |
| `/Home/Configuration` | **Configuration** hub (requires **Gis Viewer** or **Gis Admin**): see **Role behavior** below. |
| `/Home/AccessDenied` | Shown when the user is signed in but has **no** assigned GIS app role. |

**Role behavior**

| App role | Configuration UI |
|----------|-------------------|
| **Gis Viewer** (`ViewerAppRole`) | **Summary** tab only: read-only tables of Mongo data sources, GIS entities, workspaces, and access token IDs. |
| **Gis Admin** (`AdminAppRole`) | **Summary** plus tabs **1–3** (MongoDB, GIS connection, Workspace & token), including the **Configured Entities** sidebar. All mutating actions and GIS helper JSON APIs require this role. |
| *(neither)* | Redirect to **`/Home/AccessDenied`**. |

Tab **2 (GIS connection)** supports editing an existing GIS connection via the picker and **`?gisEdit={guid}`** (admins only).

**Form POSTs** (admin-only) target **`HomeController`** actions (with **`[ValidateAntiForgeryToken]`** where applicable): `SaveMongoConnection`, `SaveGisConnection`, `AssignEntitiesToWorkspace`, `SaveWorkspace`, `CreateWorkspaceAccessToken`, `UpdateWorkspaceAccessToken`.

The GIS tab also invokes JSON **POST** actions on **`HomeController`**: `ValidateMongoConnection`, `GetCollectionsForSavedConnection`, `GetMongoSampleForSavedConnection` (called from `Index.cshtml` via `fetch`).

### Admin web UI implementation (server-rendered)

The admin app is **ASP.NET Core MVC**: Razor views under **`_Backend/TdpGis.Endpoints/Views/`**, static assets under **`wwwroot/`**, Bootstrap/jQuery as in the layout and validation scripts.

## Features (summary)

- **Frontend**: workspace entity filters, debounced search against **`TdpGis.Api`**, Mapbox markers and detail overlay (BFF keeps access tokens server-side).
- **REST API (`TdpGis.Api`)**: FastEndpoints + Swagger; workspace-scoped GIS entity listing and phrase search; requires Entra JWT Bearer + `X-Access-Token`; default exception handler for consistent API errors.
- **Admin (`TdpGis.Endpoints`)**: **Microsoft Entra ID** (OIDC); **Gis Viewer** (Summary tab only) vs **Gis Admin** (full Mongo/GIS/workspace/token configuration); **`/Home/AccessDenied`** if the user has no GIS app role; **Summary** tab with read-only overview of connections, entities, workspaces, and token IDs; ASP.NET **Data Protection** keys in PostgreSQL so auth cookies survive container restarts.

## Repository layout

This `README.md` is at the repository root. The **GIS frontend** lives under **`_Frontend/`**; the **.NET solution** is under **`_Backend/`** (`TdpGisApiDemo.slnx`).
