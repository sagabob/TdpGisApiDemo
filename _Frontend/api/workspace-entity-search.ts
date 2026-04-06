import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from './verifyVercelRequest.js';
import { getWorkspaceRestConfig, workspaceUpstreamHeaders } from './workspaceRestConfig.js';
import { ensureGetOrHead, proxyUpstream } from './proxyUtils.js';

/**
 * GET /api/workspace-entity-search?entityId=&q=
 * Proxies to FastEndpoints:
 * GET {REST_API_BASE_URL}/gis-workspace/{WORKSPACE_ID}/entity/{entityId}/search/{searchedPhrase}
 * Auth: WORKSPACE_ACCESS_TOKEN as X-Access-Token (same as workspace-entities).
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (!ensureGetOrHead(req, res)) return;

  const denied = verifyIncomingRequest(req);
  if (denied) {
    return res.status(denied.status).json(denied.body);
  }

  const cfg = getWorkspaceRestConfig();
  if (!cfg.ok) {
    return res.status(cfg.status).json(cfg.body);
  }
  const { restApiBaseUrl, workspaceId, accessToken } = cfg;

  const entityId =
    typeof req.query.entityId === 'string'
      ? req.query.entityId
      : Array.isArray(req.query.entityId)
        ? req.query.entityId[0]
        : '';
  const qRaw =
    typeof req.query.q === 'string' ? req.query.q : Array.isArray(req.query.q) ? req.query.q[0] : '';

  if (!entityId?.trim()) {
    return res.status(400).json({ message: 'Missing entityId query parameter.' });
  }
  const phrase = qRaw.trim();
  if (!phrase) {
    return res.status(400).json({ message: 'Missing q query parameter.' });
  }

  const target = `${restApiBaseUrl}/gis-workspace/${workspaceId}/entity/${encodeURIComponent(entityId)}/search/${encodeURIComponent(phrase)}`;
  return proxyUpstream(req, res, {
    target,
    headers: workspaceUpstreamHeaders(accessToken),
    logTag: '[api/workspace-entity-search] upstream error',
    upstreamErrorMessage: 'Upstream workspace entity search failed.',
  });
}
