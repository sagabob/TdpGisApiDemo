import type { VercelRequest, VercelResponse } from '@vercel/node';
import {
  AUTH_ACCESS_TOKEN_COOKIE,
  AUTH_REFRESH_TOKEN_COOKIE,
  OAUTH_STATE_COOKIE,
  type EntraTokenResponse,
  ENTRA_TOKEN_REQUEST_TIMEOUT_MS,
  allowGet,
  authCookieOpts,
  authCookieSecure,
  buildSetCookie,
  entraTokenUrl,
  getEntraOAuthConfig,
  parseCookies,
  safePostLoginRedirect,
  sendOAuthErrorRedirect,
} from './oauthShared.js';

/**
 * GET /api/auth/callback — Entra redirect URI; validates `state`, exchanges `code`, sets session cookies.
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (!allowGet(req, res)) return;

  const cfg = getEntraOAuthConfig();
  if (!cfg.ok) {
    return res.status(cfg.status).json(cfg.body);
  }

  const postLogin = safePostLoginRedirect(process.env.AUTH_POST_LOGIN_REDIRECT, cfg.redirectUri);
  const secure = authCookieSecure(cfg.redirectUri);

  const q = req.query;
  const oauthError = typeof q.error === 'string' ? q.error : null;
  if (oauthError) {
    const desc = typeof q.error_description === 'string' ? q.error_description : '';
    return sendOAuthErrorRedirect(res, postLogin, cfg.redirectUri, oauthError, desc);
  }

  const code = typeof q.code === 'string' ? q.code : null;
  const state = typeof q.state === 'string' ? q.state : null;

  if (!code || !state) {
    return sendOAuthErrorRedirect(
      res,
      postLogin,
      cfg.redirectUri,
      'invalid_request',
      'Missing authorization code or state.',
    );
  }

  const cookies = parseCookies(req);
  const expectedState = cookies[OAUTH_STATE_COOKIE];
  if (!expectedState || expectedState !== state) {
    console.error('[api/auth/callback] OAuth state mismatch or missing oauth_state cookie.');
    return sendOAuthErrorRedirect(
      res,
      postLogin,
      cfg.redirectUri,
      'state_mismatch',
      'Sign-in could not be verified. Try again.',
    );
  }

  const tokenUrl = entraTokenUrl(cfg.tenantId);
  const body = new URLSearchParams({
    client_id: cfg.clientId,
    client_secret: cfg.clientSecret,
    code,
    redirect_uri: cfg.redirectUri,
    grant_type: 'authorization_code',
    scope: cfg.scope,
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
    console.error('[api/auth/callback] token request failed', e);
    return sendOAuthErrorRedirect(
      res,
      postLogin,
      cfg.redirectUri,
      'token_exchange_failed',
      'Could not reach Microsoft token endpoint.',
    );
  }

  const rawText = await tokenRes.text();
  let json: EntraTokenResponse;
  try {
    json = JSON.parse(rawText) as EntraTokenResponse;
  } catch {
    console.error('[api/auth/callback] token response not JSON', tokenRes.status, rawText.slice(0, 500));
    return sendOAuthErrorRedirect(
      res,
      postLogin,
      cfg.redirectUri,
      'token_exchange_failed',
      'Invalid token response.',
    );
  }

  if (!tokenRes.ok || !json.access_token) {
    const msg = json.error_description ?? json.error ?? rawText.slice(0, 200);
    console.error('[api/auth/callback] token exchange failed', tokenRes.status, msg);
    return sendOAuthErrorRedirect(
      res,
      postLogin,
      cfg.redirectUri,
      'token_exchange_failed',
      'Sign-in failed. Try again.',
    );
  }

  res.status(302);
  res.appendHeader('Set-Cookie', buildSetCookie(OAUTH_STATE_COOKIE, '', authCookieOpts(secure, 0)));

  const accessMaxAge = Math.max(60, json.expires_in ?? 3600);
  res.appendHeader(
    'Set-Cookie',
    buildSetCookie(AUTH_ACCESS_TOKEN_COOKIE, json.access_token, authCookieOpts(secure, accessMaxAge)),
  );

  if (json.refresh_token) {
    res.appendHeader(
      'Set-Cookie',
      buildSetCookie(
        AUTH_REFRESH_TOKEN_COOKIE,
        json.refresh_token,
        authCookieOpts(secure, 60 * 60 * 24 * 90),
      ),
    );
  }

  res.setHeader('Location', postLogin);
  res.end();
}
