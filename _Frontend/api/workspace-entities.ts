/**
 * GET /api/workspace-entities — same handler as `api/gis/workspace-entities.ts`.
 * Exposed at this path so the SPA and Vite proxy match Vercel routing.
 */
export { default } from './gis/workspace-entities.js';
