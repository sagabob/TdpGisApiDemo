import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'
import type { ProxyOptions } from 'vite'

/** Origin for proxy `target` — no defaults; set GIS_API_BASE_URL / REST_API_BASE_URL in `.env`. */
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
  const workspaceId = env.WORKSPACE_ID || ''
  const workspaceToken = env.WORKSPACE_ACCESS_TOKEN || ''

  const proxy: Record<string, string | ProxyOptions> = {}

  if (gisTarget) {
    proxy['/api/gis'] = {
      target: gisTarget,
      changeOrigin: true,
      rewrite: (p) => p.replace(/^\/api\/gis/, '/api'),
    }
  }

  if (restOrigin) {
    proxy['/api/workspace-entities'] = {
      target: restOrigin,
      changeOrigin: true,
      rewrite: () =>
        workspaceId ? `/api/gis-workspace-entities/${workspaceId}` : '/api/gis-workspace-entities',
      configure(proxyServer) {
        proxyServer.on('proxyReq', (proxyReq) => {
          if (workspaceToken) {
            proxyReq.setHeader('X-Access-Token', workspaceToken)
          }
        })
      },
    }
    proxy['/api/workspace-entity-search'] = {
      target: restOrigin,
      changeOrigin: true,
      rewrite: (reqPath) => {
        const qIdx = reqPath.indexOf('?')
        if (qIdx === -1 || !workspaceId) return reqPath
        const params = new URLSearchParams(reqPath.slice(qIdx + 1))
        const entityId = params.get('entityId')
        const q = params.get('q')
        if (!entityId || !q) return reqPath
        return `/api/gis-workspace/${workspaceId}/entity/${encodeURIComponent(entityId)}/search/${encodeURIComponent(q)}`
      },
      configure(proxyServer) {
        proxyServer.on('proxyReq', (proxyReq) => {
          if (workspaceToken) {
            proxyReq.setHeader('X-Access-Token', workspaceToken)
          }
        })
      },
    }
  }

  if (mode === 'development' && (!gisTarget || !restOrigin)) {
    console.warn(
      '[vite] Dev API proxy: set GIS_API_BASE_URL and REST_API_BASE_URL in .env (see .env.example). Missing proxy entries are skipped.',
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
