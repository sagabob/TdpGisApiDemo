import type { VercelRequest, VercelResponse } from '@vercel/node';
import {
  ENTRA_TOKEN_REQUEST_TIMEOUT_MS,
  type EntraTokenResponse,
  allowGet,
  authCookieOpts,
  authCookieSecureFromRequest,
  buildSetCookie,
  entraTokenUrl,
  GIS_API_ACCESS_TOKEN_COOKIE,
} from '../auth/oauthShared.js';
import { verifyIncomingRequest } from '../utils/verifyVercelRequest.js';

function trimEnv(key: string): string {
  return process.env[key]?.trim() ?? '';
}

type BootstrapOk = { ok: true; token: string; expiresIn: number };
type BootstrapFail = { ok: false; status: number; body: { message: string } };

async function requestClientCredentialsToken(): Promise<BootstrapOk | BootstrapFail> {
  const tenantId = trimEnv('AZURE_AD_TENANT_ID');
  const clientId = trimEnv('AZURE_AD_CLIENT_ID');
  const clientSecret = trimEnv('AZURE_AD_CLIENT_SECRET');
  const scope = trimEnv('GIS_CLIENT_CREDENTIALS_SCOPE') || trimEnv('API_SCOPE');

  if (!tenantId || !clientId || !clientSecret) {
    return {
      ok: false,
      status: 500,
      body: { message: 'Missing AZURE_AD_TENANT_ID, AZURE_AD_CLIENT_ID, or AZURE_AD_CLIENT_SECRET.' },
    };
  }
  if (!scope) {
    return {
      ok: false,
      status: 500,
      body: { message: 'Set GIS_CLIENT_CREDENTIALS_SCOPE or API_SCOPE (client credentials scope for TdpGis.Api).' },
    };
  }

  const tokenUrl = entraTokenUrl(tenantId);
  const body = new URLSearchParams({
    client_id: clientId,
    client_secret: clientSecret,
    grant_type: 'client_credentials',
    scope,
  });

  let tokenRes: Response;
  try {
    tokenRes = await fetch(tokenUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body,
      signal: AbortSignal.timeout(ENTRA_TOKEN_REQUEST_TIMEOUT_MS),
    });
  } catch (e) {
    console.error('[api/security/enable-gis-api] token request failed', e);
    return { ok: false, status: 502, body: { message: 'Could not reach Microsoft token endpoint.' } };
  }

  const rawText = await tokenRes.text();
  let json: EntraTokenResponse;
  try {
    json = JSON.parse(rawText) as EntraTokenResponse;
  } catch {
    console.error('[api/security/enable-gis-api] token response not JSON', tokenRes.status, rawText.slice(0, 300));
    return { ok: false, status: 502, body: { message: 'Invalid token response from Microsoft.' } };
  }

  if (!tokenRes.ok || !json.access_token) {
    const msg = json.error_description ?? json.error ?? rawText.slice(0, 200);
    console.error('[api/security/enable-gis-api] token exchange failed', tokenRes.status, msg);
    return {
      ok: false,
      status: 502,
      body: { message: 'Client credentials token request failed.' },
    };
  }

  const expiresIn = Math.min(Math.max(60, json.expires_in ?? 3600), 60 * 60 * 24);
  return { ok: true, token: json.access_token, expiresIn };
}

/** Internal helper for GIS handlers: issue cookie and return token. */
export async function bootstrapGisApiAccessTokenCookie(
  req: VercelRequest,
  res: VercelResponse,
): Promise<BootstrapOk | BootstrapFail> {
  const issued = await requestClientCredentialsToken();
  if (!issued.ok) return issued;

  const secure = authCookieSecureFromRequest(req);
  res.appendHeader(
    'Set-Cookie',
    buildSetCookie(GIS_API_ACCESS_TOKEN_COOKIE, issued.token, authCookieOpts(secure, issued.expiresIn)),
  );
  return issued;
}

/**
 * GET /api/security/enable-gis-api — client-credentials token for TdpGis.Api, stored in an HTTP-only cookie
 * (`gis_api_access_token`). The SPA calls this once at startup for anonymous users; signed-in users use
 * `auth_access_token` instead (see `resolveGisUpstreamBearer`).
 *
 * Env: `AZURE_AD_TENANT_ID`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`, and
 * `GIS_CLIENT_CREDENTIALS_SCOPE` or `API_SCOPE` (e.g. `api://<api-app-id>/.default`).
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (!allowGet(req, res)) return;

  const denied = verifyIncomingRequest(req);
  if (denied) {
    return res.status(denied.status).json(denied.body);
  }

  const issued = await bootstrapGisApiAccessTokenCookie(req, res);
  if (!issued.ok) {
    return res.status(issued.status).json(issued.body);
  }

  res.status(200);
  res.setHeader('Content-Type', 'application/json');
  res.setHeader('Cache-Control', 'no-store');
  return res.json({ ok: true, expiresIn: issued.expiresIn });
}
