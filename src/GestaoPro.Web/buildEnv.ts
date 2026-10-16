// Conferências do build de produção (vite.config.ts). Ver docs/deploy.md.

// VITE_API_BASE_URL é obrigatória no build: sem ela o painel chamaria a própria Vercel, e o rewrite para o
// index.html devolveria HTML no lugar dos dados. Precisa ser https (o painel é servido em https), exceto
// localhost para testar o build na máquina.
export function apiBaseUrlProblem(value: string | undefined): string | null {
  const url = value?.trim()
  if (!url) {
    return 'VITE_API_BASE_URL não definida. O build de produção precisa do endereço da API, por exemplo ' +
      'VITE_API_BASE_URL=https://gestaopro-api.onrender.com (ver docs/deploy.md).'
  }
  let parsed: URL
  try {
    parsed = new URL(url)
  } catch {
    return `VITE_API_BASE_URL inválida ("${url}"). Use o endereço completo com https://, por exemplo https://gestaopro-api.onrender.com.`
  }
  const local = ['localhost', '127.0.0.1', '[::1]'].includes(parsed.hostname)
  if (parsed.protocol !== 'https:' && !(local && parsed.protocol === 'http:')) {
    return `VITE_API_BASE_URL precisa usar https ("${url}"): o painel é servido em https e o navegador bloqueia chamadas http.`
  }
  return null
}

// Endereço público do painel, para a imagem de prévia do link (og:image) sair com URL absoluta.
export function siteUrl(env: Record<string, string | undefined>): string {
  const explicit = env.VITE_SITE_URL?.trim()
  if (explicit) return explicit.replace(/\/+$/, '')
  const vercel = env.VERCEL_PROJECT_PRODUCTION_URL?.trim()
  return vercel ? `https://${vercel.replace(/\/+$/, '')}` : ''
}

export function absoluteOgImage(html: string, site: string): string {
  if (!site) return html
  return html.replace(
    '<meta property="og:image" content="/og-image.png" />',
    `<meta property="og:image" content="${site}/og-image.png" />\n    <meta property="og:url" content="${site}/" />`,
  )
}
