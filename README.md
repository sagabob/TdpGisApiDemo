# TdpGisApiDemo

Sample ASP.NET Core app for configuring **MongoDB** data sources, **GIS entity** definitions (collections, mappings, geometry), **workspaces**, and **workspace access tokens**. The relational metadata is stored in **SQL Server** (Entity Framework Core); entity data itself can live in MongoDB.

## Solution layout

All projects live under `_Backend/`:

| Project | Description |
|--------|-------------|
| **TdpGis.Endpoints** | Runnable **MVC** web app (controllers + Razor views). Entry point: `Program.cs`. |
| **TdpGis.ApplicationDb** | EF Core `GisAppDbContext`, `GisDbService`, Fluent API configurations, migrations. |
| **TdpGis.Application** | Abstractions (`IGisDbService`), DTOs. |
| **TdpGis.Models** | Domain entities (connections, workspaces, access tokens, etc.). |

Open the solution:

```bash
dotnet build _Backend/TdpGisApiDemo.slnx
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **SQL Server** (local or remote) for application metadata
- **MongoDB** (optional at install time; required when you add Mongo connection strings and GIS entities that read Mongo collections)

## Configuration

1. Copy or edit `_Backend/TdpGis.Endpoints/appsettings.Development.json`.
2. Set **`ConnectionStrings:Database`** to a valid SQL Server connection string for the EF database.

For production or shared machines, prefer **environment variables**, **.NET User Secrets**, or a secret store instead of committing passwords.

Base `appsettings.json` does not define a connection string; ensure it is supplied for non-Development environments (e.g. `ConnectionStrings__Database`).

## Database

Apply EF Core migrations to create/update the SQL schema (run from repo root or `_Backend`):

```bash
dotnet ef database update --project _Backend/TdpGis.ApplicationDb/TdpGis.ApplicationDb.csproj --startup-project _Backend/TdpGis.Endpoints/TdpGis.Endpoints.csproj --context GisAppDbContext
```

> **Note:** `TdpGis.ApplicationDb` references both SQL Server and Npgsql packages; the app currently registers **SQL Server** only (`AddAppDatabaseConfiguration`).

## Run the web app

```bash
cd _Backend/TdpGis.Endpoints
dotnet run
```

Or open `_Backend/TdpGisApiDemo.slnx` in Visual Studio / Rider and start **TdpGis.Endpoints**.

Default ports (see `Properties/launchSettings.json`): **https://localhost:7036** and **http://localhost:5291**.

## UI routes

| Route | Purpose |
|-------|---------|
| `/` (`Home/Index`) | Landing / project info |
| `/Home/Configuration` | **Configuration** hub: Mongo connections, GIS connections (create/edit), workspaces, assign entities to workspaces, access tokens |

Tab **2 (GIS connection)** supports **editing** an existing GIS connection via the picker and `?gisEdit={guid}`.

## Features (summary)

- Save and validate **MongoDB** connection strings (metadata in SQL).
- Define **GIS connections**: collection, query field, geometry type, property mappings, optional workspace.
- **Workspaces**: create/rename, assign GIS entities, issue and manage **access tokens** (name, expiry, active/public flags).
- Client-side calls from the GIS tab use JSON actions on `HomeController` (collections list, sample document) with anti-forgery tokens.

## Repository root

This `README.md` sits at the repository root; the buildable solution is under **`_Backend/`**.
