# TDP GIS Map Application (Frontend)

React + Vite frontend for searching workspace GIS entities and rendering results on a Mapbox map.

## Features
- Mapbox map rendering with markers, popup details, and scale/navigation controls.
- Workspace-aware entity filters (multi-select) for narrowing search.
- Debounced text search with cancellation via `AbortController`.
- Result attribution by entity (shows entity label per record and summary counts).
- Server-side proxy/BFF pattern for backend calls (tokens stay off the client bundle).

## Tech Stack
- React 19 + TypeScript
- Vite 8
- Tailwind CSS v4
- Mapbox GL + `react-map-gl`
- Axios + Lodash debounce

## Prerequisites
- Node.js 18+ (recommended)
- npm

## Setup

1. Install dependencies:

```bash
npm install
```

2. Copy environment template (`.env.example` -> `.env`) and edit values.

3. Fill `.env` values:

```env
REST_API_BASE_URL=https://your-api-host.example/api
GIS_API_BASE_URL=https://your-gis-host.example/api
WORKSPACE_ID=...
WORKSPACE_ACCESS_TOKEN=...
VITE_MAPBOX_ACCESS_TOKEN=pk....
```

> `.env` is ignored by git. `.env.example` is safe to commit.

4. Start local dev:

```bash
npm run dev
```

Open <http://localhost:5173>.

## API Routing Model

### Local dev (`npm run dev`)
Vite proxies same-origin browser calls to backend origins from `.env`:
- `/api/gis/*`
- `/api/workspace-entities`
- `/api/workspace-entity-search?entityId=...&q=...`

`WORKSPACE_ACCESS_TOKEN` is attached server-side in the proxy request headers.

### Deployment (Vercel)
Serverless routes under `api/` handle upstream proxying:
- `api/gis/[...path].ts`
- `api/workspace-entities.ts`
- `api/workspace-entity-search.ts`

They validate incoming requests (`verifyVercelRequest.ts`) and read server env only.

## Search Flow
- `useWorkspaceEntities` loads available workspace entities.
- `useWorkspaceGeoSearch` calls `/api/workspace-entity-search` for each selected entity.
- Responses are normalized by `mapWorkspaceSearchResults.ts`.
- Dropdown and map consume unified `GeoFeature[]` from context.

## Scripts
- `npm run dev` - run Vite dev server
- `npm run build` - type-check and build production assets
- `npm run preview` - preview built assets
- `npm run lint` - lint project
