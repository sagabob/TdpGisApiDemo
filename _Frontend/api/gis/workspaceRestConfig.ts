/**
 * Shared validation for Vercel routes that proxy to the REST API using workspace env
 * (`REST_API_BASE_URL`, `WORKSPACE_ID`, `WORKSPACE_ACCESS_TOKEN`).
 */

export type WorkspaceRestConfigReady = {
  ok: true;
  restApiBaseUrl: string;
  workspaceId: string;
  accessToken: string;
};

export type WorkspaceRestConfigError = {
  ok: false;
  status: 500;
  body: { message: string };
};

export type WorkspaceRestConfigResult = WorkspaceRestConfigReady | WorkspaceRestConfigError;

/**
 * Reads and validates server env for workspace-scoped REST proxy routes.
 * Returns a structured error if any required variable is missing.
 */
export function getWorkspaceRestConfig(): WorkspaceRestConfigResult {
  const restApiBaseUrl = process.env.REST_API_BASE_URL?.trim().replace(/\/$/, '') ?? '';
  const workspaceId = process.env.WORKSPACE_ID?.trim() ?? '';
  const accessToken = process.env.WORKSPACE_ACCESS_TOKEN?.trim() ?? '';

  if (!restApiBaseUrl) {
    return {
      ok: false,
      status: 500,
      body: { message: 'REST_API_BASE_URL is not configured on the server.' },
    };
  }
  if (!workspaceId) {
    return {
      ok: false,
      status: 500,
      body: { message: 'WORKSPACE_ID is not configured on the server.' },
    };
  }
  if (!accessToken) {
    return {
      ok: false,
      status: 500,
      body: { message: 'WORKSPACE_ACCESS_TOKEN is not configured on the server.' },
    };
  }

  return { ok: true, restApiBaseUrl, workspaceId, accessToken };
}

/** Headers for upstream FastEndpoints calls that expect `X-Access-Token`. */
export function workspaceUpstreamHeaders(accessToken: string): Record<string, string> {
  return {
    Accept: 'application/json',
    'X-Access-Token': accessToken,
  };
}
