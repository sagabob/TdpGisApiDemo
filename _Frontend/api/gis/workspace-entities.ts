import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from '../utils/verifyVercelRequest.js';
import { getWorkspaceRestConfig, workspaceUpstreamHeaders } from './workspaceRestConfig.js';
import { ensureGetOrHead, proxyUpstream } from '../utils/proxyUtils.js';

/**
 * GET /api/workspace-entities — proxies to FastEndpoints GIS workspace list.
 * Upstream URL and token are server-only (Vercel env: REST_API_BASE_URL, WORKSPACE_ID, WORKSPACE_ACCESS_TOKEN).
 * Optional incoming checks: ALLOWED_ORIGINS, INTERNAL_API_KEY — see `api/verifyVercelRequest.ts`.
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
  const target = `${restApiBaseUrl}/gis-workspace-entities/${workspaceId}`;
  return proxyUpstream(req, res, {
    target,
    headers: workspaceUpstreamHeaders(accessToken),
    logTag: '[api/workspace-entities] upstream error',
    upstreamErrorMessage: 'Upstream workspace entities request failed.',
  });
}
