import type { ViewState } from 'react-map-gl/mapbox'

const defaultPosition: ViewState = {
  longitude: 172.639847,
  latitude: -43.525650,
  zoom: 9,
  bearing: 0,
  pitch: 0,
  padding: { top: 0, bottom: 0, left: 0, right: 0 },
}

/**
 * Browser only calls same-origin `/api/*` routes. Real upstream URLs and tokens
 * are configured in Vercel (or Vite dev proxy env — never `VITE_*` for secrets).
 *
 * GIS entity list + text search go through REST (`/api/gis/workspace-entities`,
 * `/api/gis/workspace-entity-search`), not legacy `/api/gis/GisQuery/*` routes.
 */
/** Workspace-scoped text search (Vercel / dev proxy → REST `gis-workspace/.../entity/.../search/...`). */
const workspaceEntitySearchUrl = '/api/gis/workspace-entity-search'

/** Mapbox requires a public token in the client; restrict by URL in the Mapbox dashboard. */
const mapboxAccessToken = import.meta.env.VITE_MAPBOX_ACCESS_TOKEN || ''

const defaultPinColor = '#d00'
const selectedPinColor = '#3b82f6'

const workspaceEntitiesUrl = '/api/gis/workspace-entities'

export {
  defaultPosition,
  mapboxAccessToken,
  workspaceEntitySearchUrl,
  defaultPinColor,
  selectedPinColor,
  workspaceEntitiesUrl,
}
