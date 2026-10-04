import { clearStoredSession } from '../auth/session'

export const API_BASE = import.meta.env.VITE_API_BASE_URL || (import.meta.env.PROD ? 'https://gestaopro-api-99vx.onrender.com' : '')

export function endpoint(path: string) {
  return `${API_BASE}${path}`
}

export const SESSION_EXPIRED_EVENT = 'gestaopro:session-expired'

// Servidor gratuito "dormindo" (cold start): se uma requisição passa deste tempo, o painel avisa que está
// acordando o servidor em vez de parecer travado. SERVER_SLOW_EVENT/SERVER_READY_EVENT levam a contagem.
export const SERVER_SLOW_EVENT = 'gestaopro:server-slow'
export const SERVER_READY_EVENT = 'gestaopro:server-ready'
export const SLOW_REQUEST_MS = 2500
let slowRequests = 0

async function trackSlow<T>(work: Promise<T>): Promise<T> {
  let slow = false
  const timer = setTimeout(() => {
    slow = true
    slowRequests++
    window.dispatchEvent(new Event(SERVER_SLOW_EVENT))
  }, SLOW_REQUEST_MS)
  try {
    return await work
  } finally {
    clearTimeout(timer)
    if (slow && --slowRequests === 0) window.dispatchEvent(new Event(SERVER_READY_EVENT))
  }
}

export async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const token = localStorage.getItem('gestaopro_token')
  if (token && !headers.has('Authorization')) headers.set('Authorization', `Bearer ${token}`)

  const response = await trackSlow(fetch(endpoint(path), {
    ...init,
    headers,
  }))

  if (!response.ok) {
    const text = await response.text()
    if (response.status === 401) {
      clearStoredSession()
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
      throw new Error('Sua sessão expirou. Entre novamente.')
    }
    throw new Error(formatApiError(text, response.status))
  }

  if (response.status === 204) return undefined as T
  return response.json()
}


export function isRegistrationLike(value: string) {
  return /^COND-\d{4}-\d{4}$/i.test(value.trim())
}

export async function resolveLoggedUserName(userId: string, fallback: string) {
  if (!userId) return fallback
  try {
    const user = await requestJson<Record<string, unknown>>(`/api/Users/${encodeURIComponent(userId)}`)
    return String(user.name ?? fallback)
  } catch {
    return fallback
  }
}

export function formatApiError(text: string, status: number) {
  if (status === 403) return 'Você não tem permissão para realizar esta ação.'
  if (status === 404) return 'Registro não encontrado.'
  // Erro do servidor (ProblemDetails): nunca mostra o título técnico em inglês.
  if (status >= 500) return 'Ocorreu um erro inesperado. Tente novamente.'
  if (!text) return 'Não foi possível concluir a operação. Tente novamente.'

  try {
    const data = JSON.parse(text) as { message?: string; title?: string; errors?: Record<string, string[]> }
    if (data.message) return data.message
    if (data.errors) {
      if (Object.keys(data.errors).some(key => key.toLowerCase().includes('birthdate'))) {
        return 'Data de nascimento inválida. Informe a data no formato DD/MM/AAAA.'
      }
      return 'Verifique os dados informados.'
    }
    if (data.title && !data.title.toLowerCase().includes('validation errors')) return data.title
  } catch {
    if (text.length < 180 && !text.includes('<') && !text.includes(' at ')) return text
  }

  return status >= 500
    ? 'Ocorreu um erro inesperado. Tente novamente.'
    : 'Não foi possível concluir a operação. Verifique os dados informados.'
}
