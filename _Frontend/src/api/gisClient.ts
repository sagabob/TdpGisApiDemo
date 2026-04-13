import axios from 'axios';

/**
 * Shared client for same-origin `/api/gis/*` BFF routes.
 *
 * `withCredentials: true` is required so **every** GIS request includes HTTP-only cookies
 * (`gis_api_access_token` from `/api/security/enable-gis-api`, and `auth_access_token` when signed in).
 * The BFF reads those cookies and sets `Authorization: Bearer` on upstream TdpGis.Api calls.
 */
export const gisApiClient = axios.create({
  withCredentials: true,
  headers: {
    Accept: 'application/json',
  },
});
