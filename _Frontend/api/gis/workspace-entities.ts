import type { VercelRequest, VercelResponse } from '@vercel/node';
import { verifyIncomingRequest } from '../utils/verifyVercelRequest.js';
import {
  getPublicWorkspaceRestConfig,
  getPrivateWorkspaceCredentials,
  resolveGisUpstreamBearer,
  workspaceUpstreamHeaders,
} from './workspaceRestConfig.js';
import { ensureGetOrHead } from '../utils/proxyUtils.js';
import { getEntraAccessTokenFromRequest, isRequestAuthenticated } from '../auth/oauthShared.js';
import { bootstrapGisApiAccessTokenCookie } from '../security/enable-gis-api.js';

type EntityRecord = Record<string, unknown> & {
  id?: string;
  workspaceId?: string;
  isPrivate?: boolean;
};

function annotateWithWorkspace(
  items: unknown[],
  workspaceId: string,
  options?: { isPrivate?: boolean },
): EntityRecord[] {
  const isPrivate = options?.isPrivate === true;
  return items.map((raw) => {
    const base =
      raw && typeof raw === 'object'
        ? ({ ...(raw as object), workspaceId } as EntityRecord)
        : ({ workspaceId } as EntityRecord);
    if (isPrivate) base.isPrivate = true;
    return base;
  });
}

function mergeKey(workspaceId: string, entityId: string): string {
  return `${workspaceId}\0${entityId}`;
}

async function fetchWorkspaceEntitiesJson(
  credentials: {
    restApiBaseUrl: string;
    workspaceId: string;
    accessToken: string;
  },
  req: VercelRequest,
  explicitBearer?: string,
): Promise<unknown[]> {
  const target = `${credentials.restApiBaseUrl}/gis-workspace-entities/${credentials.workspaceId}`;
  const upstream = await fetch(target, {
    method: 'GET',
    headers: workspaceUpstreamHeaders(credentials.accessToken, req, explicitBearer),
    signal: AbortSignal.timeout(25_000),
  });
  if (!upstream.ok) {
    console.error('[api/gis/workspace-entities] upstream', upstream.status, credentials.workspaceId);
    const err = new Error(`Upstream workspace entities failed (${upstream.status})`) as Error & {
      upstreamStatus: number;
    };
    err.upstreamStatus = upstream.status;
    throw err;
  }
  const data: unknown = await upstream.json();
  if (!Array.isArray(data)) throw new Error('Invalid workspace entities response');
  return data;
}

/**
 * GET /api/gis/workspace-entities — always returns the **public** workspace list; when the browser has a
 * Microsoft session cookie, also merges the **private** workspace list server-side.
 * Each item includes `workspaceId` for `/api/gis/workspace-entity-search` routing.
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (!ensureGetOrHead(req, res)) return;

  const denied = verifyIncomingRequest(req);
  if (denied) {
    return res.status(denied.status).json(denied.body);
  }

  const pub = getPublicWorkspaceRestConfig();
  if (!pub.ok) {
    return res.status(pub.status).json(pub.body);
  }

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

  res.setHeader('Content-Type', 'application/json');
  res.setHeader('Cache-Control', 'no-store');

  try {
    const publicRaw = await fetchWorkspaceEntitiesJson(pub, req, bootstrapBearer);
    const merged: EntityRecord[] = annotateWithWorkspace(publicRaw, pub.workspaceId);

    if (isRequestAuthenticated(req)) {
      const priv = getPrivateWorkspaceCredentials();
      if (priv) {
        try {
          const privateRaw = await fetchWorkspaceEntitiesJson(priv, req, bootstrapBearer);
          const privateAnnotated = annotateWithWorkspace(privateRaw, priv.workspaceId, {
            isPrivate: true,
          });
          const seen = new Set(
            merged.map((e) => mergeKey(String(e.workspaceId ?? ''), String(e.id ?? ''))),
          );
          for (const row of privateAnnotated) {
            const k = mergeKey(String(row.workspaceId ?? ''), String(row.id ?? ''));
            if (!seen.has(k)) {
              seen.add(k);
              merged.push(row);
            }
          }
        } catch (e) {
          console.error('[api/gis/workspace-entities] private workspace fetch failed', e);
        }
      }
    }

    return res.status(200).json(merged);
  } catch (e) {
    console.error('[api/gis/workspace-entities] upstream error', e);
    const upstreamStatus =
      e && typeof e === 'object' && 'upstreamStatus' in e && typeof (e as { upstreamStatus: unknown }).upstreamStatus === 'number'
        ? (e as { upstreamStatus: number }).upstreamStatus
        : undefined;
    return res.status(502).json({
      message: 'Upstream workspace entities request failed.',
      ...(upstreamStatus != null && { upstreamStatus }),
      ...(upstreamStatus === 401 && {
        hint: getEntraAccessTokenFromRequest(req)
          ? 'Signed-in token was rejected — ensure AUTH_SCOPE includes the TdpGis.Api scope and the token has app role TdpGisApi.Access (or your AzureAd:ApiAccessAppRole).'
          : 'Bearer from REST_API_BEARER_TOKEN / PUBLIC_API_BEARER_TOKEN was rejected — use a valid client-credentials token for this API (audience + role), or sign in with a user token.',
      }),
    });
  }
}
