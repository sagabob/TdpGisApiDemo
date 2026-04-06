import type { VercelRequest } from '@vercel/node';
import { timingSafeEqual } from 'node:crypto';

export type RequestDenied = { status: number; body: Record<string, string> };

/**
 * Optional guards for BFF routes (set in Vercel env, not VITE_*).
 *
 * - `ALLOWED_ORIGINS` — comma-separated exact origins (e.g. `https://myapp.vercel.app,http://localhost:5173`).
 *   When set, the request must send `Origin` or `Referer` matching one entry (stops other sites from calling your `/api` from the browser).
 * - `INTERNAL_API_KEY` — when set, requires header `x-internal-key: <key>` or `Authorization: Bearer <key>`.
 *   Intended for server-to-server calls; do not put the key in the SPA bundle if you need real secrecy.
 */
export function verifyIncomingRequest(req: VercelRequest): RequestDenied | null {
  const originDenied = verifyAllowedOrigins(req);
  if (originDenied) return originDenied;

  const keyDenied = verifyInternalApiKey(req);
  if (keyDenied) return keyDenied;

  return null;
}

function verifyAllowedOrigins(req: VercelRequest): RequestDenied | null {
  const allowList = process.env.ALLOWED_ORIGINS?.split(',')
    .map((s) => s.trim())
    .filter(Boolean);
  if (!allowList?.length) return null;

  const originHeader = req.headers.origin;
  const origin = typeof originHeader === 'string' ? originHeader : null;

  let refererOrigin: string | null = null;
  const ref = req.headers.referer;
  if (typeof ref === 'string') {
    try {
      refererOrigin = new URL(ref).origin;
    } catch {
      refererOrigin = null;
    }
  }

  const effective = origin ?? refererOrigin;
  if (!effective) {
    return { status: 403, body: { message: 'Origin or Referer required.' } };
  }

  const allowed = allowList.some((o) => effective === o);
  if (!allowed) {
    return { status: 403, body: { message: 'Forbidden origin.' } };
  }

  return null;
}

function verifyInternalApiKey(req: VercelRequest): RequestDenied | null {
  const secret = process.env.INTERNAL_API_KEY?.trim();
  if (!secret) return null;

  const headerVal = req.headers['x-internal-key'];
  const fromHeader = typeof headerVal === 'string' ? headerVal : null;
  const auth = req.headers.authorization;
  const fromBearer =
    typeof auth === 'string' && auth.startsWith('Bearer ') ? auth.slice(7).trim() : null;

  const provided = fromHeader ?? fromBearer;
  if (!provided) {
    return { status: 401, body: { message: 'Missing internal API key.' } };
  }

  try {
    const a = Buffer.from(secret, 'utf8');
    const b = Buffer.from(provided, 'utf8');
    if (a.length !== b.length || !timingSafeEqual(a, b)) {
      return { status: 403, body: { message: 'Invalid internal API key.' } };
    }
  } catch {
    return { status: 403, body: { message: 'Invalid internal API key.' } };
  }

  return null;
}
