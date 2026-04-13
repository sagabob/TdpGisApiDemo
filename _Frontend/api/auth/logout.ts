import type { VercelRequest, VercelResponse } from '@vercel/node';
import {
  AUTH_ACCESS_TOKEN_COOKIE,
  AUTH_REFRESH_TOKEN_COOKIE,
  OAUTH_STATE_COOKIE,
  GIS_API_ACCESS_TOKEN_COOKIE,
  allowGet,
  authCookieOpts,
  authCookieSecureFromRequest,
  buildSetCookie,
  safePostLoginRedirect,
} from './oauthShared.js';

/** GET /api/auth/logout — clears auth cookies and redirects. */
export default function handler(req: VercelRequest, res: VercelResponse) {
  if (!allowGet(req, res)) return;

  const redirectUri = process.env.AUTH_REDIRECT_URI?.trim() ?? '';
  const secure = authCookieSecureFromRequest(req);
  const location = redirectUri
    ? safePostLoginRedirect(process.env.AUTH_POST_LOGIN_REDIRECT, redirectUri)
    : '/';

  const clear = (name: string) => buildSetCookie(name, '', authCookieOpts(secure, 0));

  res.status(302);
  res.appendHeader('Set-Cookie', clear(AUTH_ACCESS_TOKEN_COOKIE));
  res.appendHeader('Set-Cookie', clear(AUTH_REFRESH_TOKEN_COOKIE));
  res.appendHeader('Set-Cookie', clear(OAUTH_STATE_COOKIE));
  res.appendHeader('Set-Cookie', clear(GIS_API_ACCESS_TOKEN_COOKIE));
  res.setHeader('Location', location);
  res.end();
}
