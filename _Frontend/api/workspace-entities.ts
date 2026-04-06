import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from '../verifyVercelRequest';
import { getWorkspaceRestConfig, workspaceUpstreamHeaders } from './workspaceRestConfig';

/**
 * GET /api/workspace-entities — proxies to FastEndpoints GIS workspace list.
 * Upstream URL and token are server-only (Vercel env: REST_API_BASE_URL, WORKSPACE_ID, WORKSPACE_ACCESS_TOKEN).
 * Optional incoming checks: ALLOWED_ORIGINS, INTERNAL_API_KEY — see `verifyVercelRequest.ts`.
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

  const target = `${restApiBaseUrl}/gis-workspace-entities/${workspaceId}`;

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
    console.error('[api/workspace-entities] upstream error', e);
    return res.status(502).json({ message: 'Upstream workspace entities request failed.' });
  }
}
