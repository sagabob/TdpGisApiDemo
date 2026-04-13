import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from '../utils/verifyVercelRequest.js';
import { ensureGetOrHead, proxyUpstream } from '../utils/proxyUtils.js';

/**
 * Optional legacy proxy: GET /api/gis/* → `{GIS_API_BASE_URL}/*` (Vercel env only).
 * The SPA uses REST workspace routes (`/api/gis/workspace-entities`, `/api/gis/workspace-entity-search`) instead;
 * keep this only if something still calls `/api/gis/...` upstream.
 * Optional GIS_API_ACCESS_TOKEN adds X-Access-Token upstream.
 * Optional incoming checks: ALLOWED_ORIGINS, INTERNAL_API_KEY — see `api/verifyVercelRequest.ts`.
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (!ensureGetOrHead(req, res)) return;

  const denied = verifyIncomingRequest(req);
  if (denied) {
    return res.status(denied.status).json(denied.body);
  }

  const base = process.env.GIS_API_BASE_URL;
  if (!base?.trim()) {
    return res.status(500).json({
      message: 'GIS_API_BASE_URL is not configured on the server.',
    });
  }

  const pathFromQuery = req.query.path;
  let pathPart =
    Array.isArray(pathFromQuery) ? pathFromQuery.join('/') : pathFromQuery ?? '';

  if (!pathPart && req.url) {
    const m = req.url.match(/^\/api\/gis\/(.+?)(?:\?|$)/);
    if (m) pathPart = decodeURIComponent(m[1]);
  }

  if (pathPart.includes('..')) {
    return res.status(400).json({ message: 'Invalid path.' });
  }

  const search = req.url?.includes('?')
    ? req.url.slice(req.url.indexOf('?'))
    : '';

  const target = `${base.replace(/\/$/, '')}/${pathPart}${search}`;

  const forwardHeaders: Record<string, string> = {
    Accept: (req.headers.accept as string) || 'application/json',
  };
  const serverToken = process.env.GIS_API_ACCESS_TOKEN?.trim();
  if (serverToken) {
    forwardHeaders['X-Access-Token'] = serverToken;
  }

  return proxyUpstream(req, res, {
    target,
    headers: forwardHeaders,
    logTag: '[api/gis] upstream error',
    upstreamErrorMessage: 'Upstream GIS request failed.',
  });
}
