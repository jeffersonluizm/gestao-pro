/// <reference types="vitest" />
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { resolveProxyTarget } from './proxyTarget'
import { absoluteOgImage, apiBaseUrlProblem, siteUrl } from './buildEnv'

export default defineConfig(({ command, mode }) => {
  const env = { ...loadEnv(mode, process.cwd(), ''), ...process.env }
  const apiTarget = resolveProxyTarget(env)

  // Build de produção sem a URL da API (ou sem https) falha aqui, com mensagem clara (ver docs/deploy.md).
  if (command === 'build' && mode === 'production') {
    const problem = apiBaseUrlProblem(env.VITE_API_BASE_URL)
    if (problem) throw new Error(`[gestaopro] ${problem}`)
  }

  const site = siteUrl(env)

  return {
    plugins: [
      react(),
      { name: 'gestaopro-og-image', transformIndexHtml: (html: string) => absoluteOgImage(html, site) },
    ],
    server: {
      port: 3000,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
        '/notificacaoHub': { target: apiTarget, changeOrigin: true, ws: true },
      },
    },
    test: {
      environment: 'jsdom',
    },
  }
})
