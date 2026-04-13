import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'
import type { ProxyOptions } from 'vite'

/** Origin for proxy `target` — no defaults; set env base URLs in `.env` (see `.env.example`). */
function originFromEnvBase(envValue: string | undefined): string | null {
  const raw = envValue?.trim()
  if (!raw) return null
  try {
    const withProtocol = /^[a-z]+:\/\//i.test(raw) ? raw : `https://${raw}`
    return new URL(withProtocol).origin
  } catch {
    return null
  }
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  const gisTarget = originFromEnvBase(env.GIS_API_BASE_URL)
  const restOrigin = originFromEnvBase(env.REST_API_BASE_URL)
  const publicWorkspaceId =
    env.PUBLIC_WORKSPACE_ID || env.WORKSPACE_ID || env.WORKSPACE_ID_PUBLIC || ''
  const publicWorkspaceToken =
    env.PUBLIC_WORKSPACE_ACCESS_TOKEN ||
    env.WORKSPACE_ACCESS_TOKEN ||
    env.WORKSPACE_ACCESS_TOKEN_PUBLIC ||
    ''
  const privateWorkspaceId = env.PRIVATE_WORKSPACE_ID || env.WORKSPACE_ID_PRIVATE || ''
  const privateWorkspaceToken =
    env.PRIVATE_WORKSPACE_ACCESS_TOKEN || env.WORKSPACE_ACCESS_TOKEN_PRIVATE || ''
  const restApiBearerToken =
    (env.REST_API_BEARER_TOKEN || env.PUBLIC_API_BEARER_TOKEN)?.trim() ?? ''

  /** Match `resolveGisUpstreamBearer`: GIS client-credentials cookie first, then user OAuth cookie. */
  function decodeCookie(name: string, cookieHeader: string | undefined): string {
    if (!cookieHeader) return ''
    const m = new RegExp(`(?:^|;\\s*)${name}=([^;]+)`).exec(cookieHeader)
    if (!m) return ''
    try {
      return decodeURIComponent(m[1].trim())
    } catch {
      return m[1].trim()
    }
  }
  function entraBearerFromRequestCookie(cookieHeader: string | undefined): string {
    return decodeCookie('gis_api_access_token', cookieHeader) || decodeCookie('auth_access_token', cookieHeader)
  }

  const proxy: Record<string, string | ProxyOptions> = {}

  // Register `/api/gis/workspace-entities` before `/api/gis` so the REST proxy wins over the GIS catch-all.
  if (restOrigin) {
    proxy['/api/gis/workspace-entities'] = {
      target: restOrigin,
      changeOrigin: true,
      rewrite: () =>
        publicWorkspaceId
          ? `/api/gis-workspace-entities/${publicWorkspaceId}`
          : '/api/gis-workspace-entities',
      configure(proxyServer) {
        proxyServer.on('proxyReq', (proxyReq, req) => {
          if (publicWorkspaceToken) {
            proxyReq.setHeader('X-Access-Token', publicWorkspaceToken)
          }
          const bearer = entraBearerFromRequestCookie(req.headers.cookie) || restApiBearerToken
          if (bearer) {
            proxyReq.setHeader('Authorization', `Bearer ${bearer}`)
          }
        })
      },
    }
    proxy['/api/gis/workspace-entity-search'] = {
      target: restOrigin,
      changeOrigin: true,
      rewrite: (reqPath) => {
        const qIdx = reqPath.indexOf('?')
        if (qIdx === -1) return reqPath
        const params = new URLSearchParams(reqPath.slice(qIdx + 1))
        const entityId = params.get('entityId')
        const q = params.get('q')
        const wsParam = params.get('workspaceId')
        const workspaceId = wsParam || publicWorkspaceId
        if (!entityId || !q || !workspaceId) return reqPath
        return `/api/gis-workspace/${workspaceId}/entity/${encodeURIComponent(entityId)}/search/${encodeURIComponent(q)}`
      },
      configure(proxyServer) {
        proxyServer.on('proxyReq', (proxyReq, req) => {
          const url = req.url ?? ''
          const qIdx = url.indexOf('?')
          const params = new URLSearchParams(qIdx >= 0 ? url.slice(qIdx + 1) : '')
          const wsParam = params.get('workspaceId')
          const token =
            privateWorkspaceId && wsParam === privateWorkspaceId
              ? privateWorkspaceToken
              : publicWorkspaceToken
          if (token) {
            proxyReq.setHeader('X-Access-Token', token)
          }
          const bearer = entraBearerFromRequestCookie(req.headers.cookie) || restApiBearerToken
          if (bearer) {
            proxyReq.setHeader('Authorization', `Bearer ${bearer}`)
          }
        })
      },
    }
  }

  if (gisTarget) {
    proxy['/api/gis'] = {
      target: gisTarget,
      changeOrigin: true,
      rewrite: (p) => p.replace(/^\/api\/gis/, '/api'),
    }
  }

  if (mode === 'development' && !restOrigin) {
    console.warn(
      '[vite] Dev API proxy: set REST_API_BASE_URL in `.env` for workspace entities/search (see .env.example).',
    )
  }

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      proxy,
    },
  }
})
