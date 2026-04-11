import { randomBytes } from 'node:crypto';
import type { VercelRequest, VercelResponse } from '@vercel/node';
import {
  OAUTH_STATE_COOKIE,
  allowGet,
  authCookieOpts,
  authCookieSecure,
  buildSetCookie,
  entraAuthorizeUrl,
  getEntraOAuthConfig,
} from './oauthShared.js';

/** GET /api/auth/login — starts Entra authorize flow (CSRF `oauth_state` cookie + redirect). */
export default function handler(req: VercelRequest, res: VercelResponse) {
  if (!allowGet(req, res)) return;

  const cfg = getEntraOAuthConfig();
  if (!cfg.ok) {
    return res.status(cfg.status).json(cfg.body);
  }

  const state = randomBytes(32).toString('base64url');
  const secure = authCookieSecure(cfg.redirectUri);

  res.setHeader(
    'Set-Cookie',
    buildSetCookie(OAUTH_STATE_COOKIE, state, authCookieOpts(secure, 600)),
  );

  const params = new URLSearchParams({
    client_id: cfg.clientId,
    response_type: 'code',
    redirect_uri: cfg.redirectUri,
    response_mode: 'query',
    scope: cfg.scope,
    state,
  });

  const location = entraAuthorizeUrl(cfg.tenantId, params);
  res.status(302).setHeader('Location', location).end();
}
