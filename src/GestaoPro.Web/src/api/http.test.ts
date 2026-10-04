import { describe, expect, it } from 'vitest'
import { formatApiError } from './http'
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
})

describe('safeReturnPath', () => {
  it.each(['https://site-malicioso.exemplo', '//site-malicioso.exemplo', '/entrar', null])('recusa destino fora do painel: %s', value => {
    expect(safeReturnPath(value)).toBe('/painel')
  })

  it('aceita rota do painel', () => {
    expect(safeReturnPath('/painel/avisos')).toBe('/painel/avisos')
  })
})
