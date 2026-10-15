import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import { calls, renderApp, stubFetch } from './helpers'

// GET /api/demo traz só papéis, nomes e o código do condomínio. A entrada é por POST /api/demo/entrar:
// nenhuma senha passa pelo painel nem aparece na tela.
const demo = {
  condominiumName: 'Residencial Demonstração',
  condominiumCode: 'Codigo-Da-Demo-2026',
  accounts: [
    { role: 'sindico', name: 'Ana Ribeiro' },
    { role: 'morador', name: 'Maria Souza' },
    { role: 'porteiro', name: 'Carlos Pereira' },
  ],
  preRegistered: [{ cpf: '111.111.111-11', name: 'João da Silva' }],
}

const sessao = (role: string, userName: string) => ({
  token: 'jwt', userId: `u-${role}`, userName, registrationNumber: `${role}@demo.gestaopro.local`,
  role, condominiumId: 'c-1', mustChangePassword: false,
})

beforeEach(() => {
  localStorage.clear()
  sessionStorage.clear()
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  vi.useRealTimers()
})

describe('modo demonstração', () => {
  it('sem o modo demo (404) o login não mostra o cartão', async () => {
    stubFetch((_method, path) => (path === '/api/demo' ? { status: 404, body: {} } : undefined))
    renderApp('/entrar')

    await waitFor(() => expect(calls.some(call => call.path === '/api/demo')).toBe(true))
    expect(screen.queryByRole('region', { name: /Explore a demonstração/ })).toBeNull()
  })

  it('entra como síndico com um clique, sem senha nenhuma', async () => {
    stubFetch((method, path) => {
      if (path === '/api/demo') return demo
      if (method === 'POST' && path === '/api/demo/entrar') return sessao('sindico', 'Ana Ribeiro')
      return undefined
    })
    const { user } = renderApp('/entrar')

    const card = await screen.findByRole('region', { name: /Explore a demonstração/ })
    expect(within(card).queryByText(/senha/i)).toBeNull()
    await user.click(within(card).getByRole('button', { name: /Entrar como síndico/ }))

    expect(await screen.findByRole('heading', { name: /Painel da gestão/ })).toBeTruthy()
    expect(calls).toContainEqual(expect.objectContaining({ method: 'POST', path: '/api/demo/entrar', body: { papel: 'sindico' } }))
    expect(calls.some(call => call.path === '/api/Auth/login')).toBe(false)
  })

  it('a landing oferece os três papéis e mostra o código e os CPFs para testar a ativação', async () => {
    stubFetch((_method, path) => (path === '/api/demo' ? demo : undefined))
    const { user } = renderApp('/')

    const card = await screen.findByRole('region', { name: /Explore a demonstração/ })
    for (const papel of ['síndico', 'morador', 'porteiro']) {
      expect(within(card).getByRole('button', { name: new RegExp(`Entrar como ${papel}`) })).toBeTruthy()
    }
    await user.click(within(card).getByText(/Testar a ativação de acesso/))
    expect(within(card).getByText('Codigo-Da-Demo-2026')).toBeTruthy()
    expect(within(card).getByText('111.111.111-11')).toBeTruthy()
    expect(within(card).queryByText(/@demo\.gestaopro\.local/)).toBeNull()
  })

  it('demonstração ainda sendo preparada (503): tenta de novo uma vez e entra', async () => {
    let tentativas = 0
    stubFetch((method, path) => {
      if (path === '/api/demo') return demo
      if (method === 'POST' && path === '/api/demo/entrar') {
        tentativas++
        return tentativas === 1 ? { status: 503, body: { message: 'A demonstração está sendo preparada.' } } : sessao('morador', 'Maria Souza')
      }
      return undefined
    })
    const { user } = renderApp('/entrar')

    const card = await screen.findByRole('region', { name: /Explore a demonstração/ })
    await user.click(within(card).getByRole('button', { name: /Entrar como morador/ }))

    await waitFor(() => expect(tentativas).toBe(2), { timeout: 4000 })
    expect(await screen.findByText(/Olá, Maria/, undefined, { timeout: 4000 })).toBeTruthy()
  })

  it('sessão demo encerrada pelo reset (401): o cartão recarrega e entra de novo uma vez', async () => {
    sessionStorage.setItem('gestaopro_demo_role', 'morador')
    stubFetch((method, path) => {
      if (path === '/api/demo') return demo
      if (method === 'POST' && path === '/api/demo/entrar') return sessao('morador', 'Maria Souza')
      return undefined
    })
    renderApp('/entrar?motivo=sessao')

    expect(await screen.findByText(/Olá, Maria/, undefined, { timeout: 4000 })).toBeTruthy()
    expect(calls.filter(call => call.path === '/api/demo/entrar')).toHaveLength(1)
  })

  it('não entra de novo sozinho mais de uma vez seguida', async () => {
    sessionStorage.setItem('gestaopro_demo_role', 'morador')
    sessionStorage.setItem('gestaopro_demo_retry', String(Date.now() - 5000))
    stubFetch((_method, path) => (path === '/api/demo' ? demo : undefined))
    renderApp('/entrar?motivo=sessao')

    await screen.findByRole('region', { name: /Explore a demonstração/ })
    expect(calls.some(call => call.path === '/api/demo/entrar')).toBe(false)
  })

  it('servidor demorando a responder mostra o aviso de que está acordando', async () => {
    stubFetch()
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => undefined)))
    renderApp('/entrar')

    expect(await screen.findByText(/Acordando o servidor de demonstração/, undefined, { timeout: 4000 })).toBeTruthy()
  })
})
