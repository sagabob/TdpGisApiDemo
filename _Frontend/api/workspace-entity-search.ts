import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from '../verifyVercelRequest';
import { getWorkspaceRestConfig, workspaceUpstreamHeaders } from './workspaceRestConfig';

/**
 * GET /api/workspace-entity-search?entityId=&q=
 * Proxies to FastEndpoints:
 * GET {REST_API_BASE_URL}/gis-workspace/{WORKSPACE_ID}/entity/{entityId}/search/{searchedPhrase}
 * Auth: WORKSPACE_ACCESS_TOKEN as X-Access-Token (same as workspace-entities).
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    res.setHeader('Allow', 'GET, HEAD');
    return res.status(405).end();
  }

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

  try {
    const upstream = await fetch(target, {
      method: req.method,
      headers: workspaceUpstreamHeaders(accessToken),
      signal: AbortSignal.timeout(25_000),
    });

    const contentType = upstream.headers.get('content-type');
    if (contentType) res.setHeader('Content-Type', contentType);

    res.status(upstream.status);
    const buf = Buffer.from(await upstream.arrayBuffer());
    return res.send(buf);
  } catch (e) {
    console.error('[api/workspace-entity-search] upstream error', e);
    return res.status(502).json({ message: 'Upstream workspace entity search failed.' });
  }
}
