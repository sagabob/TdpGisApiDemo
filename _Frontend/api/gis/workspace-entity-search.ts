import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from '../utils/verifyVercelRequest.js';
import {
  resolveGisUpstreamBearer,
  resolveWorkspaceForSearch,
  workspaceUpstreamHeaders,
} from './workspaceRestConfig.js';
import { ensureGetOrHead, proxyUpstream } from '../utils/proxyUtils.js';
import { bootstrapGisApiAccessTokenCookie } from '../security/enable-gis-api.js';

/**
 * GET /api/gis/workspace-entity-search?entityId=&q=&workspaceId=
 * Proxies to FastEndpoints:
 * GET {REST}/gis-workspace/{workspaceId}/entity/{entityId}/search/{phrase}
 * Optional `workspaceId` matches entities returned from merged public + private workspace lists.
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (!ensureGetOrHead(req, res)) return;

  const denied = verifyIncomingRequest(req);
  if (denied) {
    return res.status(denied.status).json(denied.body);
  }

  const workspaceIdParam =
    typeof req.query.workspaceId === 'string'
      ? req.query.workspaceId
      : Array.isArray(req.query.workspaceId)
        ? req.query.workspaceId[0]
        : undefined;

  const cfg = resolveWorkspaceForSearch(workspaceIdParam);
  if (!cfg.ok) {
    return res.status(cfg.status).json(cfg.body);
  }
  const { restApiBaseUrl, workspaceId, accessToken } = cfg;
  let bootstrapBearer: string | undefined;

  if (!resolveGisUpstreamBearer(req)) {
    const issued = await bootstrapGisApiAccessTokenCookie(req, res);
    if (!issued.ok) {
      return res.status(503).json({
        message: 'No Entra bearer token available for TdpGis.Api.',
        hint:
          'Auto-bootstrap failed. Configure GIS_CLIENT_CREDENTIALS_SCOPE/API_SCOPE and Entra client credentials, or call /api/security/enable-gis-api manually.',
      });
    }
    bootstrapBearer = issued.token;
  }

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
    headers: workspaceUpstreamHeaders(accessToken, req, bootstrapBearer),
    logTag: '[api/gis/workspace-entity-search] upstream error',
    upstreamErrorMessage: 'Upstream workspace entity search failed.',
  });
}
