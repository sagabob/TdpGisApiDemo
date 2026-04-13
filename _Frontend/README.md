# TDP GIS Map Application (Frontend)

React + Vite frontend for querying workspace GIS entities through `TdpGis.Api` and rendering results on a Mapbox map.

Live demo: [https://tdp-gis-api-demo.vercel.app/](https://tdp-gis-api-demo.vercel.app/)

## Features
- Mapbox map rendering with markers, popup details, and scale/navigation controls.
- Workspace-aware entity filters (multi-select) for narrowing search.
- Debounced text search with cancellation via `AbortController`.
- Result attribution by entity (shows entity label per record and summary counts).
- BFF/server-proxy pattern so workspace and API tokens never go into the client bundle.
- Two data modes:
  - Anonymous visitors: public workspace only.
  - Signed-in users: public + private workspace merged server-side.

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

2. Copy `.env.example` to `.env` and fill values.

3. For local development:
- If you need only SPA + Vite proxy behavior, run:
```bash
npm run dev
```
- If you need serverless `api/*` routes (auth/session/cookie bootstrap), run:
```bash
vercel dev
```

> `.env` is ignored by git. `.env.example` is safe to commit.

## Current API Routes (Frontend BFF)
- `GET /api/gis/workspace-entities`
- `GET /api/gis/workspace-entity-search?entityId=...&q=...&workspaceId=...`
- `GET /api/security/enable-gis-api` (client-credentials bootstrap; sets HTTP-only `gis_api_access_token`)
- `GET /api/auth/login`
- `GET /api/auth/callback`
- `GET /api/auth/session`
- `GET /api/auth/logout`

## Auth and Token Flow
- Browser never receives raw GIS API tokens in JSON payloads.
- `GET /api/security/enable-gis-api` fetches Entra client-credentials token and sets HTTP-only cookie `gis_api_access_token`.
- Sign-in flow sets HTTP-only `auth_access_token`.
- GIS upstream bearer resolution order is:
  1. `gis_api_access_token` cookie
  2. `auth_access_token` cookie
  3. `REST_API_BEARER_TOKEN` / `PUBLIC_API_BEARER_TOKEN` env fallback
- GIS routes auto-bootstrap if bearer is missing, then use the freshly issued token for the same request.

## Troubleshooting
- First search returns empty/error then second works:
  - fixed in current handlers by using freshly bootstrapped bearer in the same request.
- `No Entra bearer token available for TdpGis.Api`:
  - verify `AZURE_AD_TENANT_ID`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`
  - set `GIS_CLIENT_CREDENTIALS_SCOPE` (or `API_SCOPE`) to your API client-credentials scope.
- Signed in but no private workspace data:
  - verify `PRIVATE_WORKSPACE_ID` and `PRIVATE_WORKSPACE_ACCESS_TOKEN` are set on the BFF runtime.

## Public vs Signed-In Behavior
- Public user:
  - Can load public workspace entities/search.
  - No private workspace merge.
- Signed-in user:
  - Still gets public workspace entities.
  - If `PRIVATE_WORKSPACE_ID` + `PRIVATE_WORKSPACE_ACCESS_TOKEN` are configured, private workspace entities are merged in `api/gis/workspace-entities.ts`.

## Local Runtime Behavior
### `npm run dev` (Vite proxy)
- Proxies `/api/gis/*`, `/api/gis/workspace-entities`, `/api/gis/workspace-entity-search`.
- Useful for UI/dev proxy work.
- Does not execute serverless route files directly.

### `vercel dev` / deployed Vercel
- Executes `api/*` serverless handlers.
- Required for full auth/session/bootstrap flow and private merge behavior tied to HTTP-only cookies.

## Search/Data Flow
- `useWorkspaceEntities`:
  - Ensures GIS bootstrap cookie (`ensureGisApiCookie`) then loads `/api/gis/workspace-entities`.
  - Uses separate session caches for `anon` and `auth`.
- `useWorkspaceGeoSearch` calls `/api/gis/workspace-entity-search` per selected entity.
- Responses are normalized by `mapWorkspaceSearchResults.ts`.
- Dropdown and map consume unified `GeoFeature[]` from context.

## Scripts
- `npm run dev` - run Vite dev server
- `npm run build` - type-check and build production assets
- `npm run preview` - preview built assets
- `npm run lint` - lint project
