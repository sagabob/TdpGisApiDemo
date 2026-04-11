import type { VercelRequest, VercelResponse } from '@vercel/node';
import { AUTH_ACCESS_TOKEN_COOKIE, allowGet, parseCookies } from './oauthShared.js';

/** GET /api/auth/session — `{ authenticated }` from HTTP-only cookie (for SPA Sign in / Sign out). */
export default function handler(req: VercelRequest, res: VercelResponse) {
  if (!allowGet(req, res)) return;

  const cookies = parseCookies(req);
  const token = cookies[AUTH_ACCESS_TOKEN_COOKIE];
  const authenticated = Boolean(token?.length);

  res.setHeader('Content-Type', 'application/json');
  res.setHeader('Cache-Control', 'no-store');
  return res.status(200).json({ authenticated });
}
