import type { VercelRequest, VercelResponse } from '@vercel/node';

export type EntraOAuthConfigReady = {
  ok: true;
  tenantId: string;
  clientId: string;
  clientSecret: string;
  /** Full callback URL registered in Entra (e.g. https://app.vercel.app/api/auth/callback). */
  redirectUri: string;
  /** Space-separated scopes for authorize + token requests. */
  scope: string;
};

export type EntraOAuthConfigError = {
  ok: false;
  status: number;
  body: { message: string };
};

export type EntraOAuthConfigResult = EntraOAuthConfigReady | EntraOAuthConfigError;

const DEFAULT_SCOPE = 'openid profile email offline_access';

/** HTTP-only session cookies set by `api/auth/callback.ts` and cleared by `api/auth/logout.ts`. */
export const AUTH_ACCESS_TOKEN_COOKIE = 'auth_access_token';
export const AUTH_REFRESH_TOKEN_COOKIE = 'auth_refresh_token';
export const OAUTH_STATE_COOKIE = 'oauth_state';

export function authCookieSecure(redirectUri: string): boolean {
  return redirectUri.startsWith('https://');
}

/**
 * Prefer `AUTH_REDIRECT_URI` (matches how login sets cookies); fall back to `x-forwarded-proto` for logout when env is missing.
 */
export function authCookieSecureFromRequest(req: VercelRequest): boolean {
  const redirectUri = process.env.AUTH_REDIRECT_URI?.trim() ?? '';
  if (redirectUri) return authCookieSecure(redirectUri);
  const xf = req.headers['x-forwarded-proto'];
  return typeof xf === 'string' && xf.split(',')[0]?.trim() === 'https';
}

/** Shared shape for Microsoft Entra `/token` JSON (success and error bodies). */
export type EntraTokenResponse = {
  token_type?: string;
  expires_in?: number;
  access_token?: string;
  refresh_token?: string;
  id_token?: string;
  scope?: string;
  error?: string;
  error_description?: string;
};

export const ENTRA_TOKEN_REQUEST_TIMEOUT_MS = 25_000;

export function allowGet(req: VercelRequest, res: VercelResponse): boolean {
  if (req.method !== 'GET') {
    res.setHeader('Allow', 'GET');
    res.status(405).end();
    return false;
  }
  return true;
}

export type CookieOpts = {
  maxAge: number;
  httpOnly: boolean;
  sameSite: 'Lax' | 'Strict' | 'None';
  path: string;
  secure: boolean;
};

/** HTTP-only session cookies: login, callback, logout. */
export function authCookieOpts(secure: boolean, maxAge: number): CookieOpts {
  return {
    maxAge,
    httpOnly: true,
    sameSite: 'Lax',
    path: '/',
    secure,
  };
}

/** Single `Set-Cookie` header value (RFC 6265). */
export function buildSetCookie(name: string, value: string, opts: CookieOpts): string {
  const parts = [
    `${encodeURIComponent(name)}=${encodeURIComponent(value)}`,
    `Path=${opts.path}`,
    `Max-Age=${opts.maxAge}`,
  ];
  if (opts.httpOnly) parts.push('HttpOnly');
  parts.push(`SameSite=${opts.sameSite}`);
  if (opts.secure) parts.push('Secure');
  return parts.join('; ');
}

export function entraAuthorizeUrl(tenantId: string, params: URLSearchParams): string {
  return `https://login.microsoftonline.com/${encodeURIComponent(tenantId)}/oauth2/v2.0/authorize?${params.toString()}`;
}

export function entraTokenUrl(tenantId: string): string {
  return `https://login.microsoftonline.com/${encodeURIComponent(tenantId)}/oauth2/v2.0/token`;
}

/**
 * Server-only Microsoft Entra ID OAuth settings (set in Vercel env / local `vercel dev`, not `VITE_*`).
 */
export function getEntraOAuthConfig(): EntraOAuthConfigResult {
  const tenantId = process.env.AZURE_AD_TENANT_ID?.trim() ?? '';
  const clientId = process.env.AZURE_AD_CLIENT_ID?.trim() ?? '';
  const clientSecret = process.env.AZURE_AD_CLIENT_SECRET?.trim() ?? '';
  const redirectUri = process.env.AUTH_REDIRECT_URI?.trim() ?? '';
  const scope = (process.env.AUTH_SCOPE?.trim() || DEFAULT_SCOPE).replace(/\s+/g, ' ');

  if (!tenantId) {
    return { ok: false, status: 500, body: { message: 'AZURE_AD_TENANT_ID is not configured on the server.' } };
  }
  if (!clientId) {
    return { ok: false, status: 500, body: { message: 'AZURE_AD_CLIENT_ID is not configured on the server.' } };
  }
  if (!clientSecret) {
    return { ok: false, status: 500, body: { message: 'AZURE_AD_CLIENT_SECRET is not configured on the server.' } };
  }
  if (!redirectUri) {
    return { ok: false, status: 500, body: { message: 'AUTH_REDIRECT_URI is not configured on the server.' } };
  }

  try {
    new URL(redirectUri);
  } catch {
    return { ok: false, status: 500, body: { message: 'AUTH_REDIRECT_URI must be a valid absolute URL.' } };
  }

  return { ok: true, tenantId, clientId, clientSecret, redirectUri, scope };
}

export function parseCookies(req: VercelRequest): Record<string, string> {
  const raw = req.headers.cookie;
  if (!raw) return {};
  const out: Record<string, string> = {};
  for (const segment of raw.split(';')) {
    const idx = segment.indexOf('=');
    const name = (idx === -1 ? segment : segment.slice(0, idx)).trim();
    if (!name) continue;
    const value = idx === -1 ? '' : segment.slice(idx + 1).trim();
    try {
      out[decodeURIComponent(name)] = decodeURIComponent(value);
    } catch {
      out[name] = value;
    }
  }
  return out;
}

/**
 * Where to send the browser after login or on error. Only same-origin paths or full URLs
 * matching the auth redirect origin are allowed (avoids open redirects).
 */
export function safePostLoginRedirect(raw: string | undefined, authRedirectUri: string): string {
  const fallback = '/';
  const v = raw?.trim();
  if (!v) return fallback;

  let appOrigin: string;
  try {
    appOrigin = new URL(authRedirectUri).origin;
  } catch {
    return fallback;
  }

  if (v.startsWith('/') && !v.startsWith('//')) {
    return v;
  }

  try {
    const u = new URL(v);
    if (u.origin === appOrigin) {
      return u.pathname + u.search + u.hash;
    }
  } catch {
    /* ignore */
  }

  return fallback;
}

export function buildErrorRedirectLocation(
  postLogin: string,
  authRedirectUri: string,
  error: string,
  description: string,
): string {
  const baseOrigin = new URL(authRedirectUri).origin;
  const target = postLogin.startsWith('http')
    ? new URL(postLogin)
    : new URL(postLogin, baseOrigin);
  target.searchParams.set('error', error);
  if (description) target.searchParams.set('error_description', description);
  if (postLogin.startsWith('http')) return target.href;
  return target.pathname + target.search + target.hash;
}

export function sendOAuthErrorRedirect(
  res: VercelResponse,
  postLogin: string,
  authRedirectUri: string,
  error: string,
  description: string,
): void {
  const loc = buildErrorRedirectLocation(postLogin, authRedirectUri, error, description);
  res.status(302).setHeader('Location', loc).end();
}
