import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import { calls, loginAs, renderApp, stubFetch } from './helpers'

beforeEach(() => {
  localStorage.clear()
  stubFetch()
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

const loginOk = (role: string, extra: Record<string, unknown> = {}) => ({
  token: 'jwt', userId: role === 'morador' ? 'u-maria' : 'u-admin', userName: role === 'morador' ? 'Maria' : 'Ana',
  registrationNumber: `${role}@condominio.com`, role, condominiumId: 'c-1', mustChangePassword: false, ...extra,
})

describe('rotas', () => {
  it('página inexistente mostra o 404 com caminho de volta', async () => {
    renderApp('/caminho/que/nao/existe')

    expect(await screen.findByRole('heading', { name: /Página não encontrada/ })).toBeTruthy()
    expect(screen.getByRole('link', { name: /Voltar ao início/ }).getAttribute('href')).toBe('/')
  })

  it('o painel sem sessão leva para /entrar guardando a rota de volta', async () => {
    renderApp('/painel/avisos')

    expect(await screen.findByRole('heading', { name: 'Entrar' })).toBeTruthy()
  })

  it('morador que abre um módulo da gestão vê a página de sem permissão', async () => {
    loginAs('morador')
    renderApp('/painel/usuarios')

    expect(await screen.findByRole('heading', { name: /Sem permissão/ })).toBeTruthy()
  })

  it('a landing pública apresenta o produto e leva ao login', async () => {
    renderApp('/')

    expect(screen.getByRole('heading', { level: 1 }).textContent).toMatch(/condomínio/i)
    expect(screen.getAllByRole('link', { name: /Entrar/ })[0].getAttribute('href')).toBe('/entrar')
  })
})

describe('entrar', () => {
  it('não pede tipo de acesso: o papel vem do token', async () => {
    renderApp('/entrar')

    expect(screen.getByLabelText('E-mail')).toBeTruthy()
    expect(screen.getByLabelText('Senha')).toBeTruthy()
    expect(screen.queryByLabelText(/Tipo de acesso/)).toBeNull()
  })

  it('síndico entra direto na gestão e vê o painel', async () => {
    stubFetch((method, path) => (method === 'POST' && path === '/api/Auth/login' ? loginOk('sindico') : undefined))
    const { user } = renderApp('/entrar')

    await user.type(screen.getByLabelText('E-mail'), 'sindico@condominio.com')
    await user.type(screen.getByLabelText('Senha'), 'Senha-Forte-2026')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('heading', { name: /Painel da gestão/ })).toBeTruthy()
    expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/Auth/login',
      body: { registrationNumber: 'sindico@condominio.com', password: 'Senha-Forte-2026' },
    }))
  })

  it('erro de login aparece numa região anunciada ao leitor de tela', async () => {
    stubFetch((method, path) => (method === 'POST' && path === '/api/Auth/login'
      ? { status: 400, body: { message: 'E-mail ou senha inválidos.' } } : undefined))
    const { user } = renderApp('/entrar')

    await user.type(screen.getByLabelText('E-mail'), 'x@y.com')
    await user.type(screen.getByLabelText('Senha'), 'errada-123')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect((await screen.findByRole('alert')).textContent).toMatch('E-mail ou senha inválidos.')
  })

  it('primeiro acesso leva à troca de senha com confirmação e depois entra', async () => {
    let logins = 0
    stubFetch((method, path) => {
      if (method === 'POST' && path === '/api/Auth/login') {
        logins++
        return logins === 1 ? { mustChangePassword: true, userId: 'u-maria' } : loginOk('morador')
      }
      if (method === 'PATCH' && path === '/api/Auth/change-password') return { status: 204 }
      return undefined
    })
    const { user } = renderApp('/entrar')

    await user.type(screen.getByLabelText('E-mail'), 'maria@condominio.com')
    await user.type(screen.getByLabelText('Senha'), 'Codigo-Inicial-2026')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('heading', { name: 'Primeiro acesso' })).toBeTruthy()
    await user.type(screen.getByLabelText('Nova senha'), 'Minha-Senha-2026')
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Minha-Senha-2026')
    await user.click(screen.getByRole('button', { name: 'Salvar e entrar' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'PATCH', path: '/api/Auth/change-password',
      body: { userId: 'u-maria', currentPassword: 'Codigo-Inicial-2026', newPassword: 'Minha-Senha-2026' },
    })))
    expect(await screen.findByRole('heading', { name: /Olá, Maria/ })).toBeTruthy()
  })

  it('sessão expirada volta ao login com aviso', async () => {
    loginAs('morador')
    stubFetch((_method, path) => (path.startsWith('/api/announcements') ? { status: 401, body: {} } : undefined))
    renderApp('/painel/avisos')

    expect(await screen.findByText(/Sua sessão expirou/)).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Entrar' })).toBeTruthy()
    expect(localStorage.getItem('gestaopro_token')).toBeNull()
  })
})

describe('cadastro em dois passos', () => {
  it('confere CPF e código, pede os dados que faltam e cadastra com o e-mail informado', async () => {
    stubFetch((method, path) => {
      if (method === 'GET' && path === '/api/Condominiums/public') return [{ id: 'c-1', name: 'Residencial Jardim' }]
      if (method === 'POST' && path === '/api/auth/check-cpf') return { canProceed: true, needsProfileData: true, message: 'ok' }
      if (method === 'POST' && path === '/api/auth/register') return { registrationNumber: 'maria@email.com', name: 'Maria Souza' }
      return undefined
    })
    const { user } = renderApp('/cadastro')

    await user.selectOptions(await screen.findByLabelText('Condomínio'), 'c-1')
    await user.type(screen.getByLabelText('CPF'), '11111111111')
    await user.type(screen.getByLabelText('Código do condomínio'), 'Jardim-Azul-2026')
    await user.click(screen.getByRole('button', { name: 'Continuar' }))

    await user.type(await screen.findByLabelText('Nome completo'), 'Maria Souza')
    await user.type(screen.getByLabelText('E-mail'), 'maria@email.com')
    await user.type(screen.getByLabelText('Senha'), 'Minha-Senha-2026')
    await user.type(screen.getByLabelText('Confirmar senha'), 'Minha-Senha-2026')
    await user.click(screen.getByRole('button', { name: 'Criar acesso' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/auth/register',
      body: expect.objectContaining({ cpf: '11111111111', condominiumId: 'c-1', condominiumCode: 'Jardim-Azul-2026', email: 'maria@email.com', name: 'Maria Souza', password: 'Minha-Senha-2026' }),
    })))
    expect(await screen.findByText('maria@email.com')).toBeTruthy()
  })

  it('senha fraca é avisada antes de enviar', async () => {
    stubFetch((method, path) => {
      if (method === 'GET' && path === '/api/Condominiums/public') return [{ id: 'c-1', name: 'Residencial Jardim' }]
      if (method === 'POST' && path === '/api/auth/check-cpf') return { canProceed: true, needsProfileData: false, message: 'ok' }
      return undefined
    })
    const { user } = renderApp('/cadastro')

    await user.selectOptions(await screen.findByLabelText('Condomínio'), 'c-1')
    await user.type(screen.getByLabelText('CPF'), '11111111111')
    await user.type(screen.getByLabelText('Código do condomínio'), 'Jardim-Azul-2026')
    await user.click(screen.getByRole('button', { name: 'Continuar' }))
    await user.type(await screen.findByLabelText('Senha'), '12345678')
    await user.type(screen.getByLabelText('Confirmar senha'), '12345678')
    await user.click(screen.getByRole('button', { name: 'Criar acesso' }))

    expect((await screen.findByRole('alert')).textContent).toMatch(/8 a 128 caracteres/)
    expect(calls.some(call => call.path === '/api/auth/register')).toBe(false)
  })
})

describe('recuperar senha', () => {
  it('explica o caminho real (administração redefine) sem fingir que manda e-mail', () => {
    renderApp('/recuperar-senha')

    expect(screen.getByRole('heading', { name: 'Esqueci minha senha' })).toBeTruthy()
    expect(screen.getAllByText(/administração do condomínio/).length).toBeGreaterThan(0)
    expect(screen.getByRole('link', { name: 'Voltar para Entrar' }).getAttribute('href')).toBe('/entrar')
    expect(screen.queryByRole('textbox', { name: /e-mail/i })).toBeNull()
  })
})

describe('perfil', () => {
  it('troca a própria senha pela rota do usuário', async () => {
    loginAs('morador')
    stubFetch((method, path) => {
      if (method === 'GET' && path === '/api/Users/u-maria') return { id: 'u-maria', name: 'Maria', email: 'maria@condominio.com', role: 'morador' }
      if (method === 'PATCH' && path === '/api/Users/u-maria/change-password') return { status: 204 }
      return undefined
    })
    const { user } = renderApp('/painel/perfil')

    const form = (await screen.findByRole('heading', { name: 'Trocar senha' })).closest('section')!
    await user.type(within(form).getByLabelText('Senha atual'), 'Senha-Antiga-2026')
    await user.type(within(form).getByLabelText('Nova senha'), 'Senha-Nova-2026')
    await user.type(within(form).getByLabelText('Confirmar nova senha'), 'Senha-Nova-2026')
    await user.click(within(form).getByRole('button', { name: 'Trocar senha' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'PATCH', path: '/api/Users/u-maria/change-password',
      body: { currentPassword: 'Senha-Antiga-2026', newPassword: 'Senha-Nova-2026' },
    })))
    expect(await screen.findByText('Senha alterada.')).toBeTruthy()
  })
})
