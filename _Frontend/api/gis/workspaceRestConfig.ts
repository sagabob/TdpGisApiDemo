import type { VercelRequest } from '@vercel/node';
import { getEntraAccessTokenFromRequest, getGisApiAccessTokenFromRequest } from '../auth/oauthShared.js';

/**
 * Shared validation for Vercel routes that proxy to the REST API using workspace env.
 *
 * Public workspace: `PUBLIC_WORKSPACE_ID` + `PUBLIC_WORKSPACE_ACCESS_TOKEN`, or legacy
 * `WORKSPACE_ID` + `WORKSPACE_ACCESS_TOKEN`, or alternate `WORKSPACE_ID_PUBLIC` /
 * `WORKSPACE_ACCESS_TOKEN_PUBLIC`.
 *
 * Private workspace: optional `PRIVATE_WORKSPACE_ID` + `PRIVATE_WORKSPACE_ACCESS_TOKEN`
 * (or `WORKSPACE_ID_PRIVATE` + `WORKSPACE_ACCESS_TOKEN_PRIVATE`).
 * (merged server-side when the user has an auth cookie).
 */

export type WorkspaceRestConfigReady = {
  ok: true;
  restApiBaseUrl: string;
  workspaceId: string;
  accessToken: string;
};

export type WorkspaceRestConfigError = {
  ok: false;
  status: number;
  body: { message: string };
};

export type WorkspaceRestConfigResult = WorkspaceRestConfigReady | WorkspaceRestConfigError;

export type WorkspaceCredentials = {
  restApiBaseUrl: string;
  workspaceId: string;
  accessToken: string;
};

function trimEnv(key: string): string {
  return process.env[key]?.trim() ?? '';
}

/** REST API origin (no trailing slash). */
export function getRestApiBaseUrl(): string {
  return trimEnv('REST_API_BASE_URL').replace(/\/$/, '');
}

/**
 * Public workspace credentials — used for anonymous users and as the base list for signed-in users.
 */
export function getPublicWorkspaceRestConfig(): WorkspaceRestConfigResult {
  const restApiBaseUrl = getRestApiBaseUrl();
  if (!restApiBaseUrl) {
    return {
      ok: false,
      status: 500,
      body: { message: 'REST_API_BASE_URL is not configured on the server.' },
    };
  }
  const workspaceId =
    trimEnv('PUBLIC_WORKSPACE_ID') || trimEnv('WORKSPACE_ID') || trimEnv('WORKSPACE_ID_PUBLIC');
  const accessToken =
    trimEnv('PUBLIC_WORKSPACE_ACCESS_TOKEN') ||
    trimEnv('WORKSPACE_ACCESS_TOKEN') ||
    trimEnv('WORKSPACE_ACCESS_TOKEN_PUBLIC');
  if (!workspaceId) {
    return {
      ok: false,
      status: 500,
      body: {
        message: 'PUBLIC_WORKSPACE_ID (or legacy WORKSPACE_ID) is not configured on the server.',
      },
    };
  }
  if (!accessToken) {
    return {
      ok: false,
      status: 500,
      body: {
        message:
          'PUBLIC_WORKSPACE_ACCESS_TOKEN (or legacy WORKSPACE_ACCESS_TOKEN) is not configured on the server.',
      },
    };
  }

  return { ok: true, restApiBaseUrl, workspaceId, accessToken };
}

/**
 * Optional private workspace — only merged when both vars are set and the user is authenticated.
 */
export function getPrivateWorkspaceCredentials(): WorkspaceCredentials | null {
  const restApiBaseUrl = getRestApiBaseUrl();
  if (!restApiBaseUrl) return null;
  const workspaceId = trimEnv('PRIVATE_WORKSPACE_ID') || trimEnv('WORKSPACE_ID_PRIVATE');
  const accessToken = trimEnv('PRIVATE_WORKSPACE_ACCESS_TOKEN') || trimEnv('WORKSPACE_ACCESS_TOKEN_PRIVATE');
  if (!workspaceId || !accessToken) return null;
  return { restApiBaseUrl, workspaceId, accessToken };
}

/** @deprecated Use getPublicWorkspaceRestConfig — alias for existing call sites. */
export function getWorkspaceRestConfig(): WorkspaceRestConfigResult {
  return getPublicWorkspaceRestConfig();
}

/**
 * Entra JWT for TdpGis.Api upstream calls.
 *
 * Prefer **`gis_api_access_token`** (client credentials from `/api/security/enable-gis-api`) first: it always matches
 * the API app registration. The user’s **`auth_access_token`** (OAuth) is often Graph/profile-only unless AUTH_SCOPE
 * includes the API — using it first broke GIS calls after sign-in. **`isRequestAuthenticated`** still uses
 * `auth_access_token` separately to merge the private workspace.
 */
export function resolveGisUpstreamBearer(req?: VercelRequest): string | undefined {
  if (req) {
    const gisCookie = getGisApiAccessTokenFromRequest(req);
    if (gisCookie) return gisCookie;
    const userToken = getEntraAccessTokenFromRequest(req);
    if (userToken) return userToken;
  }
  return trimEnv('REST_API_BEARER_TOKEN') || trimEnv('PUBLIC_API_BEARER_TOKEN') || undefined;
}

/**
 * Headers for upstream TdpGis.Api GIS calls: always `X-Access-Token` (workspace) and `Authorization: Bearer`.
 *
 * - **Bearer:** `gis_api_access_token` (preferred), else user `auth_access_token`, else env — see `resolveGisUpstreamBearer`.
 * - **Private workspace merge** (server): still gated by user `auth_access_token` via `isRequestAuthenticated`.
 */
export function workspaceUpstreamHeaders(
  workspaceAccessToken: string,
  req?: VercelRequest,
  explicitBearer?: string,
): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'X-Access-Token': workspaceAccessToken,
  };
  const bearer = explicitBearer || resolveGisUpstreamBearer(req);
  if (bearer) {
    headers.Authorization = `Bearer ${bearer}`;
  }
  return headers;
}

/**
 * Resolve credentials for workspace entity search. Prefer `workspaceId` from merged entity lists;
 * when omitted, uses the public workspace (same as legacy single-workspace behavior).
 */
export function resolveWorkspaceForSearch(workspaceIdParam: string | undefined): WorkspaceRestConfigResult {
  const pub = getPublicWorkspaceRestConfig();
  if (!pub.ok) return pub;

  const trimmed = workspaceIdParam?.trim();
  if (!trimmed || trimmed === pub.workspaceId) return pub;

  const priv = getPrivateWorkspaceCredentials();
  if (priv && trimmed === priv.workspaceId) {
    return { ok: true, restApiBaseUrl: priv.restApiBaseUrl, workspaceId: priv.workspaceId, accessToken: priv.accessToken };
  }

  return {
    ok: false,
    status: 400,
    body: { message: 'Unknown workspaceId for search.' },
  };
}
