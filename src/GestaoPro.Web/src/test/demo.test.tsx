import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import { calls, renderApp, stubFetch } from './helpers'

// As credenciais de demonstração vêm só de GET /api/demo (nada escrito no painel).
const demo = {
  condominiumName: 'Residencial Demonstração',
  condominiumCode: 'Codigo-Da-Demo-2026',
  accounts: [
    { role: 'sindico', name: 'Ana Ribeiro', email: 'sindico@demo.gestaopro.local', password: 'Senha-Da-Demo-2026' },
    { role: 'morador', name: 'Maria Souza', email: 'morador@demo.gestaopro.local', password: 'Senha-Da-Demo-2026' },
    { role: 'porteiro', name: 'Carlos Pereira', email: 'porteiro@demo.gestaopro.local', password: 'Senha-Da-Demo-2026' },
  ],
}

beforeEach(() => {
  localStorage.clear()
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('modo demonstração', () => {
  it('sem o modo demo (404) o login não mostra o cartão', async () => {
    stubFetch((_method, path) => (path === '/api/demo' ? { status: 404, body: {} } : undefined))
    renderApp('/entrar')

    await waitFor(() => expect(calls.some(call => call.path === '/api/demo')).toBe(true))
    expect(screen.queryByRole('region', { name: /Explore a demonstração/ })).toBeNull()
    expect(screen.queryByText(/Admin@123/)).toBeNull()
  })

  it('entra como síndico com um clique usando a conta publicada pela API', async () => {
    stubFetch((method, path) => {
      if (path === '/api/demo') return demo
      if (method === 'POST' && path === '/api/Auth/login') return {
        token: 'jwt', userId: 'u-sindico', userName: 'Ana Ribeiro', registrationNumber: 'sindico@demo.gestaopro.local',
        role: 'sindico', condominiumId: 'c-1', mustChangePassword: false,
      }
      return undefined
    })
    const { user } = renderApp('/entrar')

    const card = await screen.findByRole('region', { name: /Explore a demonstração/ })
    await user.click(within(card).getByRole('button', { name: /Entrar como síndico/ }))

    expect(await screen.findByRole('heading', { name: /Painel da gestão/ })).toBeTruthy()
    expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/Auth/login',
      body: { registrationNumber: 'sindico@demo.gestaopro.local', password: 'Senha-Da-Demo-2026' },
    }))
  })

  it('a landing também oferece a demonstração com os três papéis', async () => {
    stubFetch((_method, path) => (path === '/api/demo' ? demo : undefined))
    renderApp('/')

    const card = await screen.findByRole('region', { name: /Explore a demonstração/ })
    for (const papel of ['síndico', 'morador', 'porteiro']) {
      expect(within(card).getByRole('button', { name: new RegExp(`Entrar como ${papel}`) })).toBeTruthy()
    }
    expect(within(card).getByText('Codigo-Da-Demo-2026')).toBeTruthy()
  })

  it('servidor demorando a responder mostra o aviso de que está acordando', async () => {
    stubFetch()
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => undefined)))
    renderApp('/entrar')

    expect(await screen.findByText(/Acordando o servidor de demonstração/, undefined, { timeout: 4000 })).toBeTruthy()
  })
})
