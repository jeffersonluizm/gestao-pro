import { describe, expect, it } from 'vitest'
import { apiBaseUrlProblem, absoluteOgImage, siteUrl } from './buildEnv'

// Build de produção sem a URL da API publicava um painel que chamava a própria Vercel (o rewrite devolvia HTML).
describe('apiBaseUrlProblem', () => {
  it('sem VITE_API_BASE_URL o build de produção falha com mensagem clara', () => {
    expect(apiBaseUrlProblem(undefined)).toMatch(/VITE_API_BASE_URL/)
    expect(apiBaseUrlProblem('   ')).toMatch(/VITE_API_BASE_URL/)
  })

  it('URL sem https é recusada (conteúdo misto no navegador)', () => {
    expect(apiBaseUrlProblem('http://gestaopro-api.onrender.com')).toMatch(/https/)
    expect(apiBaseUrlProblem('gestaopro-api.onrender.com')).toMatch(/https/)
  })

  it('aceita https e, para testar o build na máquina, http://localhost', () => {
    expect(apiBaseUrlProblem('https://gestaopro-api.onrender.com')).toBeNull()
    expect(apiBaseUrlProblem('http://localhost:5088')).toBeNull()
    expect(apiBaseUrlProblem('http://127.0.0.1:5088')).toBeNull()
  })
})

// A prévia do link (WhatsApp, LinkedIn) precisa da imagem com endereço completo.
describe('og:image', () => {
  it('usa VITE_SITE_URL ou o domínio de produção que a Vercel informa no build', () => {
    expect(siteUrl({ VITE_SITE_URL: 'https://gestaopro.vercel.app/' })).toBe('https://gestaopro.vercel.app')
    expect(siteUrl({ VERCEL_PROJECT_PRODUCTION_URL: 'gestaopro.vercel.app' })).toBe('https://gestaopro.vercel.app')
    expect(siteUrl({})).toBe('')
  })

  it('troca o caminho relativo pelo absoluto quando há endereço do site', () => {
    const html = '<meta property="og:image" content="/og-image.png" />'
    expect(absoluteOgImage(html, 'https://gestaopro.vercel.app')).toContain('content="https://gestaopro.vercel.app/og-image.png"')
    expect(absoluteOgImage(html, 'https://gestaopro.vercel.app')).toContain('og:url')
    expect(absoluteOgImage(html, '')).toBe(html)
  })
})
