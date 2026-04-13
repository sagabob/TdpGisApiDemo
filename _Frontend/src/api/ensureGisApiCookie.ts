/**
 * Server obtains a client-credentials token and sets HTTP-only `gis_api_access_token`
 * (see `api/security/enable-gis-api.ts`). Call before GIS `/api/gis/*` requests when the user is not signed in.
 * Requires `vercel dev` or deployed BFF — plain `vite` has no `/api` unless proxied.
 */
export async function ensureGisApiCookie(): Promise<boolean> {
  try {
    const res = await fetch('/api/security/enable-gis-api', {
      method: 'GET',
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
    });
    return res.ok;
  } catch {
    return false;
  }
}
