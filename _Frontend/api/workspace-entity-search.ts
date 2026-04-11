/**
 * GET /api/workspace-entity-search — same handler as `api/gis/workspace-entity-search.ts`.
 * Exposed at this path so the SPA and Vite proxy match Vercel routing.
 */
export { default } from './gis/workspace-entity-search.js';
