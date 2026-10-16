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

describe('painel por papel', () => {
  it('morador vê o próprio resumo: encomendas, cobranças e avisos', async () => {
    loginAs('morador')
    stubFetch((method, path) => {
      if (method === 'GET' && path === '/api/Delivery') return [{ id: 'd-1', trackingCode: 'BR123', status: 'pending', type: 'encomenda' }]
      return undefined
    })
    renderApp('/painel')

    expect(await screen.findByRole('heading', { name: /Olá, Maria/ })).toBeTruthy()
    const encomendas = await screen.findByRole('link', { name: /Encomendas aguardando/ })
    await waitFor(() => expect(within(encomendas).getByText('1')).toBeTruthy())
    expect(await screen.findByText('Manutenção do elevador')).toBeTruthy()
  })

  it('porteiro vê a portaria: visitantes no prédio e encomendas a entregar', async () => {
    loginAs('porteiro')
    renderApp('/painel')

    expect(await screen.findByRole('heading', { name: /Portaria/ })).toBeTruthy()
    expect(screen.getByRole('link', { name: /Visitantes no prédio/ })).toBeTruthy()
    expect(screen.getByRole('link', { name: /Encomendas a entregar/ })).toBeTruthy()
  })

  it('síndico pode ver o painel como morador e voltar', async () => {
    loginAs('sindico')
    const { user } = renderApp('/painel')

    expect(await screen.findByRole('heading', { name: /Painel da gestão/ })).toBeTruthy()
    await user.click(screen.getByRole('button', { name: /Ver como morador/ }))
    expect(await screen.findByRole('heading', { name: /Olá, Admin/ })).toBeTruthy()
    const nav = screen.getByRole('navigation', { name: 'Módulos' })
    expect(within(nav).queryByRole('link', { name: 'Usuários' })).toBeNull()
    await user.click(screen.getByRole('button', { name: /Voltar à gestão/ }))
    expect(await screen.findByRole('heading', { name: /Painel da gestão/ })).toBeTruthy()
  })
})

describe('avisos', () => {
  it('morador lê os avisos em feed, sem tabela', async () => {
    loginAs('morador')
    renderApp('/painel/avisos')

    const aviso = await screen.findByRole('article', { name: 'Manutenção do elevador' })
    expect(within(aviso).getByText('O elevador social para na sexta.')).toBeTruthy()
    expect(screen.queryByRole('table')).toBeNull()
  })

  it('gestão publica aviso sem escolher condomínio (vem da sessão)', async () => {
    loginAs('admin')
    const { user } = renderApp('/painel/avisos')

    const novo = (await screen.findByRole('heading', { name: 'Novo registro' })).closest('section')!
    expect(within(novo).queryByLabelText(/Condomínio/)).toBeNull()
    await user.type(within(novo).getByLabelText(/Título/), 'Assembleia geral')
    await user.type(within(novo).getByLabelText(/Conteúdo/), 'Dia 10, às 19h.')
    await user.type(within(novo).getByLabelText(/Categoria/), 'Geral')
    await user.click(within(novo).getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/announcements',
      body: expect.objectContaining({ condominiumId: 'c-1', title: 'Assembleia geral' }),
    })))
    expect(await screen.findByText('Aviso criado com sucesso.')).toBeTruthy()
  })
})

describe('reservas e ocorrências do morador', () => {
  it('morador cancela a própria reserva com confirmação', async () => {
    loginAs('morador')
    const futura = new Date(Date.now() + 5 * 86400000).toISOString().slice(0, 10)
    stubFetch((method, path) => {
      if (method === 'GET' && path === '/api/reservations/user/u-maria')
        return [{ id: 'r-1', userId: 'u-maria', commonAreaId: 'area-1', date: futura, startTime: '18:00', endTime: '20:00', status: 'pendente' }]
      if (method === 'DELETE' && path === '/api/reservations/r-1') return { status: 204 }
      return undefined
    })
    const { user } = renderApp('/painel/reservas')

    await user.click(await screen.findByRole('button', { name: 'Cancelar reserva' }))
    const dialog = await screen.findByRole('alertdialog')
    await user.click(within(dialog).getByRole('button', { name: 'Cancelar reserva' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({ method: 'DELETE', path: '/api/reservations/r-1' })))
  })

  it('morador registra ocorrência em nome próprio', async () => {
    loginAs('morador')
    const { user } = renderApp('/painel/ocorrencias')

    const novo = (await screen.findByRole('heading', { name: 'Nova ocorrência' })).closest('section')!
    expect(within(novo).queryByLabelText(/Usuário/)).toBeNull()
    await user.type(within(novo).getByLabelText(/Título/), 'Lâmpada queimada')
    await user.type(within(novo).getByLabelText(/Descrição/), 'Hall do bloco B.')
    await user.selectOptions(within(novo).getByLabelText(/Categoria/), 'Maintenance')
    await user.selectOptions(within(novo).getByLabelText(/Prioridade/), 'Low')
    await user.click(within(novo).getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/Occurrences',
      body: expect.objectContaining({ userId: 'u-maria', condominiumId: 'c-1', title: 'Lâmpada queimada' }),
    })))
    expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Occurrences/user/u-maria' }))
  })
})

describe('encomendas', () => {
  it('porteiro acessa as encomendas e registra uma', async () => {
    loginAs('porteiro')
    const { user } = renderApp('/painel/encomendas')

    const novo = (await screen.findByRole('heading', { name: 'Novo registro' })).closest('section')!
    await user.type(within(novo).getByLabelText(/Código/), 'BR999')
    await user.selectOptions(within(novo).getByLabelText(/Morador/), 'u-maria')
    await user.click(within(novo).getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({
      method: 'POST', path: '/api/Delivery', body: expect.objectContaining({ trackingCode: 'BR999', residentId: 'u-maria' }),
    })))
  })
})

describe('usuários', () => {
  it('gestão redefine a senha de um morador com confirmação', async () => {
    loginAs('admin')
    stubFetch((method, path) => (method === 'PATCH' && path === '/api/Users/u-maria/reset-password' ? { temporaryPassword: 'Temp-Abc23-Xyz45' } : undefined))
    const { user } = renderApp('/painel/usuarios')

    const linha = (await screen.findByRole('cell', { name: 'Maria' })).closest('tr')!
    await user.click(within(linha).getByRole('button', { name: 'Redefinir senha' }))
    const dialog = await screen.findByRole('alertdialog')
    expect(within(dialog).getByText(/senha temporária/)).toBeTruthy()
    await user.click(within(dialog).getByRole('button', { name: 'Redefinir senha' }))

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({ method: 'PATCH', path: '/api/Users/u-maria/reset-password' })))
    // A senha temporária aparece uma vez, fixa na tela, para a administração entregar à pessoa.
    const aviso = await screen.findByRole('status', { name: /Senha temporária/ })
    expect(within(aviso).getByText('Temp-Abc23-Xyz45')).toBeTruthy()
  })
})
