import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, formatApiError, requestJson, UNEXPECTED_FORMAT_MESSAGE } from './http'
import { safeReturnPath } from '../pages/auth/LoginPage'

describe('formatApiError', () => {
  it('erro 500 (ProblemDetails em inglês) vira mensagem genérica em português', () => {
    const problem = JSON.stringify({ type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1', title: 'An error occurred while processing your request.', status: 500 })
    expect(formatApiError(problem, 500)).toBe('Ocorreu um erro inesperado. Tente novamente.')
  })

  it('mensagem de regra da API (400) chega como veio', () => {
    expect(formatApiError(JSON.stringify({ message: 'E-mail ou senha inválidos.' }), 400)).toBe('E-mail ou senha inválidos.')
  })

  it('429 explica que é preciso aguardar', () => {
    expect(formatApiError(JSON.stringify({ message: 'Muitas tentativas. Aguarde e tente de novo.' }), 429)).toMatch(/Aguarde/)
  })

  it('403 com mensagem da API (por exemplo, registro de exemplo da demonstração) chega como veio', () => {
    const message = 'Este é um registro de exemplo da demonstração: ele não pode ser removido. Crie outro para testar.'
    expect(formatApiError(JSON.stringify({ message }), 403)).toBe(message)
  })

  it('403 sem mensagem continua com o texto genérico de permissão', () => {
    expect(formatApiError('', 403)).toBe('Você não tem permissão para realizar esta ação.')
  })

  it('503 com mensagem da API (demonstração sendo preparada) chega como veio', () => {
    expect(formatApiError(JSON.stringify({ message: 'A demonstração está sendo preparada.' }), 503)).toBe('A demonstração está sendo preparada.')
  })
})

describe('requestJson', () => {
  afterEach(() => vi.unstubAllGlobals())

  // Sem VITE_API_BASE_URL no build, o rewrite da Vercel devolve o index.html: o painel não pode mostrar
  // "Unexpected token '<'" para quem está testando.
  it('resposta em HTML vira erro legível, sem Unexpected token', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('<!doctype html><html></html>', { status: 200, headers: { 'Content-Type': 'text/html' } })))

    const error = (await requestJson('/api/demo').catch((err: unknown) => err)) as ApiError

    expect(error).toBeInstanceOf(ApiError)
    expect(error.message).toBe(UNEXPECTED_FORMAT_MESSAGE)
    expect(error.message).not.toMatch(/Unexpected token/)
  })

  it('erro da API guarda o status para quem precisa decidir (por exemplo, tentar de novo no 503)', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ message: 'Preparando.' }), { status: 503, headers: { 'Content-Type': 'application/json' } })))

    const error = (await requestJson('/api/demo/entrar', { method: 'POST' }).catch((err: unknown) => err)) as ApiError

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(503)
  })
})

describe('safeReturnPath', () => {
  it.each(['https://site-malicioso.exemplo', '//site-malicioso.exemplo', '/entrar', null])('recusa destino fora do painel: %s', value => {
    expect(safeReturnPath(value)).toBe('/painel')
  })

  it('aceita rota do painel', () => {
    expect(safeReturnPath('/painel/avisos')).toBe('/painel/avisos')
  })
})
