import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import { calls, loginAs, renderApp, stubFetch } from './helpers'

beforeEach(() => {
  localStorage.clear()
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

// Cadastro de usuários pela gestão. A API aceita a senha em branco (gera uma senha temporária, trocada no primeiro
// acesso) ou uma senha que passe na política do servidor; os papéis são os do enum UserRole: admin, sindico,
// morador e porteiro.
describe('formulário de usuários', () => {
  it('não traz senha pré-preenchida e oferece os quatro papéis da API', async () => {
    stubFetch()
    loginAs('admin')
    renderApp('/painel/usuarios')

    const novo = (await screen.findByRole('heading', { name: 'Novo registro' })).closest('section')!
    const senha = within(novo).getByLabelText(/Senha/) as HTMLInputElement
    expect(senha.value).toBe('')
    expect(senha.required).toBe(false)

    const papel = within(novo).getByLabelText(/Perfil/) as HTMLSelectElement
    const valores = Array.from(papel.options).map(option => option.value).filter(Boolean)
    expect(valores).toEqual(['admin', 'sindico', 'morador', 'porteiro'])
  })

  it('cadastro sem senha mostra a senha temporária gerada pela API', async () => {
    stubFetch((method, path) => (method === 'POST' && path === '/api/Users'
      ? { status: 201, body: { id: 'u-novo', name: 'Paulo Porteiro', temporaryPassword: 'Temp-abcde-12345' } }
      : undefined))
    loginAs('admin')
    const { user } = renderApp('/painel/usuarios')

    const novo = (await screen.findByRole('heading', { name: 'Novo registro' })).closest('section')!
    await user.type(within(novo).getByLabelText(/Nome/), 'Paulo Porteiro')
    await user.type(within(novo).getByLabelText(/E-mail/), 'paulo@exemplo.test')
    await user.type(within(novo).getByLabelText(/Telefone/), '31999990000')
    await user.type(within(novo).getByLabelText(/CPF/), '12345678909')
    await user.selectOptions(within(novo).getByLabelText(/Perfil/), 'porteiro')
    await user.click(within(novo).getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/Users', body: expect.objectContaining({ role: 'porteiro' }),
    })))
    const enviado = calls.find(call => call.method === 'POST' && call.path === '/api/Users')!.body as Record<string, unknown>
    expect(enviado.password ?? '').toBe('')
    expect(await screen.findByText(/Temp-abcde-12345/)).toBeTruthy()
  })
})
