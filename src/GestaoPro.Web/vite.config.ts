/// <reference types="vitest" />
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { resolveProxyTarget } from './proxyTarget'

export default defineConfig(({ mode }) => {
  const env = { ...loadEnv(mode, process.cwd(), 'VITE_'), ...process.env }
  const apiTarget = resolveProxyTarget(env)

  return {
    plugins: [react()],
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
