// Destino do proxy do `npm run dev` para /api e /notificacaoHub.
// Padrão: a API local (porta 5088, ver src/GestaoPro.API/Properties/launchSettings.json).
// Para usar outra API (por exemplo a de produção no Render), defina VITE_API_PROXY_TARGET.
export const DEFAULT_API_PROXY_TARGET = 'http://localhost:5088'

export function resolveProxyTarget(env: Record<string, string | undefined>) {
  return env.VITE_API_PROXY_TARGET?.trim() || DEFAULT_API_PROXY_TARGET
}
