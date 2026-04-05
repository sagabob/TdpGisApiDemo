# TdpGisApiDemo

Sample **ASP.NET Core** solution for managing **MongoDB** data sources, **GIS connection** definitions (collections, mappings, geometry), **workspaces**, and **workspace access tokens**. Relational metadata is stored in **PostgreSQL** (Entity Framework Core); GIS payloads are read from MongoDB using saved configuration.

The backend exposes two hosts: a **cookie-authenticated MVC admin** (`TdpGis.Endpoints`) and a **FastEndpoints REST API** (`TdpGis.Api`) secured by workspace access tokens for GIS queries.

## Architecture (Clean Architecture)

The backend follows **Clean Architecture** principles: **dependency direction points inward**. Outer layers depend on inner ones; the **domain** and **application** layers stay free of databases, HTTP, or UI frameworks.

**`TdpGis.Application`** and **`TdpGis.AdminApplication`** are **sibling** use case assemblies: they do **not** depend on each other. Both depend only on **`TdpGis.Domain`**. The first models **end-user** use cases (GIS API consumers); the second models **admin** use cases (configuration and metadata management). Each exposes **ports** (interfaces) that **Infrastructure** implements.

| Layer | Project | Role |
|-------|---------|------|
| **Domain** | `TdpGis.Domain` | Entities and core types. **No** references to other projects in the solution. |
| **Application (end-user)** | `TdpGis.Application` | **Use cases** for **end users**: ports such as `IGisConfigurationService`, `IGisDataService`, plus DTOs. Depends only on **Domain**. |
| **Application (admin)** | `TdpGis.AdminApplication` | **Use cases** for **admins**: `IGisAdminAppService`, configuration **repository** contract (`IGisConfigurationRepository`), and related types. Depends only on **Domain**. |
| **Infrastructure** | `TdpGis.Infrastructure` | **Adapters**: EF Core (PostgreSQL), MongoDB drivers, repository and service implementations. Depends on **Application**, **AdminApplication**, and **Domain**; registers implementations in **`AddInfrastructure`**. |
| **Presentation / composition** | `TdpGis.Endpoints`, `TdpGis.Api` | **Hosts**: MVC controllers or FastEndpoints, HTTP concerns, authentication at the edge. They reference **Infrastructure** to compose the graph at startup and expose the app to the outside world. |

**`TdpGis.Api`** uses the **end-user** application layer; **`TdpGis.Endpoints`** uses the **admin** application layer. Both share **Infrastructure** and **Domain**; only **hosting** and **transport** differ (REST vs Razor).

## Solution layout

All backend projects live under `_Backend/`:

| Project | Description |
|--------|-------------|
| **TdpGis.Endpoints** | Runnable **MVC** app: Razor views, static assets, **cookie authentication** for the configuration UI. References **AdminApplication** and **Infrastructure**. Entry point: `Program.cs`. |
| **TdpGis.Api** | Runnable **FastEndpoints** host: GIS query endpoints, **Swagger/OpenAPI**. References **Application** and **Infrastructure**. Entry point: `Program.cs`. |
| **TdpGis.Infrastructure** | EF Core **`GisAppDbContext`**, Fluent configurations, **migrations**, PostgreSQL access, **`GisConfigurationRepository`**, MongoDB helpers (`MongoClientCache`, `MongoMetadataProvider`, **`GisMongoDataService`**). |
| **TdpGis.AdminApplication** | **Admin** use cases and UI orchestration: **`IGisAdminAppService`** / **`GisAdminAppService`** for the configuration page. |
| **TdpGis.Application** | **End-user** use cases: ports (**`IGisConfigurationService`**, **`IGisDataService`**, etc.) and shared app models/DTOs. |
| **TdpGis.Domain** | Domain entities: data sources, GIS connections, property mappings, workspaces, access tokens. |

Open the solution:

```bash
dotnet build _Backend/TdpGisApiDemo.slnx
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL** for application metadata (EF Core)
- **MongoDB** (optional until you add Mongo connection strings and GIS entities that read collections)

## Configuration

### PostgreSQL (EF Core)

Set **`ConnectionStrings:Database`** to a valid PostgreSQL connection string. This applies to both **`TdpGis.Endpoints`** and **`TdpGis.Api`** (each has its own `appsettings`; use `appsettings.Development.json` locally or **environment variables** / **user secrets** for secrets).

Base `appsettings.json` files may leave the connection string empty; ensure it is supplied at runtime (e.g. `ConnectionStrings__Database`).

### Admin dashboard sign-in (`TdpGis.Endpoints` only)

The configuration UI requires authentication. Configure **`AdminDashboard:User`** and **`AdminDashboard:Password`** (see `appsettings.json` / `AdminDashboardOptions`). If the password is not set, login shows a configuration error.

## Database

Apply EF Core migrations to create or update the schema. Migrations live in **`TdpGis.Infrastructure`**. Example (from repo root):

```bash
dotnet ef database update --project _Backend/TdpGis.Infrastructure/TdpGis.Infrastructure.csproj --startup-project _Backend/TdpGis.Endpoints/TdpGis.Endpoints.csproj --context GisAppDbContext
```

You can use **`TdpGis.Api`** as the startup project instead if you prefer; it must be able to read the same **`ConnectionStrings:Database`** at design time.

> **Note:** `TdpGis.Infrastructure` references SQL Server and Npgsql packages; the app registers **PostgreSQL** via **`UseNpgsql`** in `AddInfrastructure`.

## Run the admin web app (`TdpGis.Endpoints`)

```bash
cd _Backend/TdpGis.Endpoints
dotnet run
```

Or open `_Backend/TdpGisApiDemo.slnx` in Visual Studio / Rider and start **TdpGis.Endpoints**.

Default URLs (see `Properties/launchSettings.json`): **https://localhost:7036** and **http://localhost:5291**.

Sign in at **`/Account/Login`**, then open the configuration hub.

## Run the REST API (`TdpGis.Api`)

```bash
cd _Backend/TdpGis.Api
dotnet run
```

Default URLs (see `Properties/launchSettings.json`): **https://localhost:7255** and **http://localhost:5236**.

With the app running, open the **Swagger UI** (FastEndpoints + Swagger) at **`/swagger`** on that host.

## REST API (GIS query)

GIS endpoints require a **valid workspace access token**: header **`X-Access-Token`**, or **`Authorization: Bearer`** with the token value. Tokens are issued from the admin UI and stored in PostgreSQL with the workspace.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/gis-workspace-entities/{workspaceId}` | List GIS entity definitions (DTOs) for the workspace. |
| GET | `/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}` | Search within a GIS entity’s collection (subject to ongoing implementation). |

## Admin UI routes (`TdpGis.Endpoints`)

| Route | Purpose |
|-------|---------|
| `/` (`Home/Index`) | Public landing (**`Project`** view). |
| `/Account/Login` | Admin sign-in. |
| `/Home/Configuration` | **Configuration** hub (authenticated): Mongo connections, GIS connections, workspaces, entity assignment, access tokens. |

Tab **2 (GIS connection)** supports editing an existing GIS connection via the picker and **`?gisEdit={guid}`**.

The GIS tab uses JSON POST actions on **`HomeController`** (e.g. `ValidateMongoConnection`, `GetCollectionsForSavedConnection`, `GetMongoSampleForSavedConnection`) with **anti-forgery** tokens.

## Frontends

### Server-rendered admin (`TdpGis.Endpoints`)

The admin UI is **ASP.NET Core MVC**: Razor views under **`_Backend/TdpGis.Endpoints/Views/`**, static assets under **`wwwroot/`**, Bootstrap/jQuery as in the existing layout and scripts.

### Optional SPA (`_Frontend/`)

There is a separate **Vite + React** app (Mapbox-related dependencies) under **`_Frontend/`**. It is not part of the .NET solution; run it with **`npm install`** and **`npm run dev`** (see `package.json`) if you are consuming the REST API from a browser client.

## Features (summary)

- Save and validate **MongoDB** connection strings (metadata in PostgreSQL).
- Define **GIS connections**: collection, query field, geometry type, property mappings, optional workspace.
- **Workspaces**: create/rename, assign GIS entities, issue and manage **access tokens** (name, expiry, active/public flags).
- **REST API** for workspace-scoped GIS listing and search, with token-based access.
- **Cookie**-based admin login for the configuration UI.

## Repository root

This `README.md` is at the repository root. The buildable backend solution is under **`_Backend/`**; an optional Node client lives under **`_Frontend/`**.
