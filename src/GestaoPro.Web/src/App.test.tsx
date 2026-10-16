import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import { formatCurrency, formInitialValue, moduleForSession, modules } from './App'
import { calls, loginAs, renderApp, stubFetch } from './test/helpers'

// Abre um módulo pelo menu lateral e espera os lookups (lista de usuários) chegarem antes de interagir.
async function openModule(label: string) {
  const { user } = renderApp('/painel')
  const nav = await screen.findByRole('navigation', { name: 'Módulos' })
  await user.click(within(nav).getByRole('link', { name: label }))
  await waitFor(() => expect(calls.some(call => call.path === '/api/Users')).toBe(true))
  return user
}

beforeEach(() => {
  localStorage.clear()
  stubFetch()
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('formatCurrency', () => {
  it('formata valores em reais', () => {
    expect(formatCurrency(1234.5)).toMatch(/^R\$\s1\.234,50$/)
  })

  it('mostra traço quando não há valor', () => {
    expect(formatCurrency(null)).toBe('-')
  })
})

describe('moduleForSession', () => {
  const billsModule = modules.find(module => module.id === 'bills')!
  const session = {
    token: 't', userId: 'u-maria', userName: 'Maria', role: 'morador', accessType: 'usuario',
    condominiumId: 'c-1', registrationNumber: 'maria@condominio.com',
  }

  it('lista só as cobranças do próprio morador', () => {
    expect(moduleForSession(billsModule, session, false).listEndpoint).toBe('/api/Bills/user/u-maria')
  })

  it('deixa o administrador escolher o morador', () => {
    expect(moduleForSession(billsModule, session, true).listEndpoint).toBeUndefined()
  })

  // A portaria recebe os visitantes do condomínio (a API filtra pelo condomínio do token).
  it('porteiro lista os visitantes do condomínio e morador só os próprios', () => {
    const visitorsModule = modules.find(module => module.id === 'visitors')!
    expect(moduleForSession(visitorsModule, { ...session, role: 'porteiro' }, false).listEndpoint).toBe('/api/Visitor')
    expect(moduleForSession(visitorsModule, session, false).listEndpoint).toBe('/api/Visitor/resident/u-maria')
  })
})

describe('navegação', () => {
  it.each(['admin', 'morador'] as const)('mostra Cobranças, Pagamentos e Estacionamento para %s', async kind => {
    loginAs(kind)
    renderApp('/painel')
    const nav = await screen.findByRole('navigation', { name: 'Módulos' })
    for (const label of ['Cobranças', 'Pagamentos', 'Estacionamento']) {
      expect(within(nav).getByRole('link', { name: label })).toBeTruthy()
    }
  })

  // A API devolve 403 para porteiro em /api/Bills e /api/Payments: os módulos nem aparecem.
  it('esconde Cobranças e Pagamentos do porteiro', async () => {
    loginAs('porteiro')
    renderApp('/painel')
    const nav = await screen.findByRole('navigation', { name: 'Módulos' })
    expect(within(nav).queryByRole('link', { name: 'Cobranças' })).toBeNull()
    expect(within(nav).queryByRole('link', { name: 'Pagamentos' })).toBeNull()
    expect(within(nav).getByRole('link', { name: 'Estacionamento' })).toBeTruthy()
  })
})

describe('Condomínios', () => {
  // O código do condomínio (senha padrão) é obrigatório no cadastro e opcional na edição (em branco mantém).
  it('pede o código do condomínio, obrigatório só no cadastro', () => {
    const condominiums = modules.find(module => module.id === 'condominiums')!
    const create = condominiums.fields!.find(field => field.name === 'defaultPassword')!
    const edit = condominiums.editFields!.find(field => field.name === 'defaultPassword')!

    expect(create.type).toBe('password')
    expect(create.required).toBe(true)
    expect(create.placeholder).toMatch(/8 caracteres/)
    expect(edit.required).toBeFalsy()
  })

  // A API devolve o código mascarado (********): a edição não pode reenviar a máscara como código novo.
  it('não preenche o código na edição com o valor mascarado', () => {
    const condominiums = modules.find(module => module.id === 'condominiums')!
    const edit = condominiums.editFields!.find(field => field.name === 'defaultPassword')!

    expect(formInitialValue(edit, { defaultPassword: '********' })).toBe('')
  })
})

describe('Visitantes', () => {
  it('porteiro registra entrada e saída de visitante', async () => {
    loginAs('porteiro')
    await openModule('Visitantes')

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Visitor' })))
    expect(screen.getByRole('button', { name: 'Entrada' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Saída' })).toBeTruthy()
  })

  it('morador não vê os botões de entrada e saída', async () => {
    loginAs('morador')
    await openModule('Visitantes')

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Visitor/resident/u-maria' })))
    expect(screen.queryByRole('button', { name: 'Entrada' })).toBeNull()
  })
})

describe('Cobranças', () => {
  it('administrador consulta por morador, pode editar e não tem botão Remover (a API não tem DELETE)', async () => {
    loginAs('admin')
    const user = await openModule('Cobranças')

    const consulta = screen.getByRole('heading', { name: 'Consulta' }).closest('section')!
    const [, valueSelect] = within(consulta).getAllByRole('combobox')
    await waitFor(() => expect(within(valueSelect).getByRole('option', { name: /Maria/ })).toBeTruthy())
    await user.selectOptions(valueSelect, 'u-maria')
    await user.click(within(consulta).getByRole('button', { name: 'Consultar' }))

    expect(await within(consulta).findByText('Taxa condominial 05/2026')).toBeTruthy()
    expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Bills/user/u-maria' }))
    expect(within(consulta).getByRole('button', { name: 'Editar' })).toBeTruthy()
    expect(within(consulta).queryByRole('button', { name: 'Remover' })).toBeNull()
  })

  it('o formulário de edição tem Status e o de criação não', async () => {
    loginAs('admin')
    const user = await openModule('Cobranças')
    const novo = screen.getByRole('heading', { name: 'Novo registro' }).closest('section')!
    expect(within(novo).queryByLabelText(/Status/)).toBeNull()

    const consulta = screen.getByRole('heading', { name: 'Consulta' }).closest('section')!
    const [, valueSelect] = within(consulta).getAllByRole('combobox')
    await waitFor(() => expect(within(valueSelect).getByRole('option', { name: /Maria/ })).toBeTruthy())
    await user.selectOptions(valueSelect, 'u-maria')
    await user.click(within(consulta).getByRole('button', { name: 'Consultar' }))
    await user.click(await within(consulta).findByRole('button', { name: 'Editar' }))

    const editar = screen.getByRole('heading', { name: 'Editar registro' }).closest('section')!
    expect((within(editar).getByLabelText(/Status/) as HTMLSelectElement).value).toBe('pending')
  })

  it('morador vê só as próprias cobranças, sem formulário', async () => {
    loginAs('morador')
    await openModule('Cobranças')

    expect(await screen.findByText('Taxa condominial 05/2026')).toBeTruthy()
    expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Bills/user/u-maria' }))
    expect(screen.queryByRole('heading', { name: 'Novo registro' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Editar' })).toBeNull()
  })
})

describe('Pagamentos', () => {
  it('administrador escolhe morador e cobrança, vê os pagamentos e registra um novo', async () => {
    loginAs('admin')
    const user = await openModule('Pagamentos')

    const moradorSelect = screen.getByLabelText(/Morador/)
    await waitFor(() => expect(within(moradorSelect).getByRole('option', { name: /Maria/ })).toBeTruthy())
    await user.selectOptions(moradorSelect, 'u-maria')

    const cobrancaSelect = screen.getByLabelText(/Cobrança/)
    await waitFor(() => expect(within(cobrancaSelect).getByRole('option', { name: /Taxa condominial/ })).toBeTruthy())
    await user.selectOptions(cobrancaSelect, 'b-1')

    expect(await screen.findByRole('cell', { name: 'Pix' })).toBeTruthy()
    expect(screen.getByRole('cell', { name: /R\$\s450,50/ })).toBeTruthy()
    expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Payments/bill/b-1' }))

    const novo = screen.getByRole('heading', { name: 'Novo pagamento' }).closest('section')!
    await user.type(within(novo).getByLabelText(/Valor pago/), '450.50')
    await user.selectOptions(within(novo).getByLabelText(/Forma de pagamento/), 'pix')
    await user.click(within(novo).getByRole('button', { name: 'Registrar pagamento' }))

    await waitFor(() => expect(calls).toContainEqual({
      method: 'POST',
      path: '/api/Payments',
      body: { billId: 'b-1', amountPaid: 450.5, paymentMethod: 'pix' },
    }))
    expect(await screen.findByText('Pagamento registrado com sucesso.')).toBeTruthy()
  })

  it('morador vê as próprias cobranças sem escolher morador e não registra pagamento', async () => {
    loginAs('morador')
    await openModule('Pagamentos')

    await waitFor(() => expect(calls).toContainEqual(expect.objectContaining({ method: 'GET', path: '/api/Bills/user/u-maria' })))
    expect(screen.queryByLabelText(/Morador/)).toBeNull()
    expect(screen.queryByRole('heading', { name: 'Novo pagamento' })).toBeNull()
  })
})

describe('Estacionamento', () => {
  it('administrador cadastra vaga sem Status e edita com Status', async () => {
    loginAs('admin')
    const user = await openModule('Estacionamento')

    const novo = screen.getByRole('heading', { name: 'Novo registro' }).closest('section')!
    expect(within(novo).queryByLabelText(/Status/)).toBeNull()

    await user.click(await screen.findByRole('button', { name: 'Editar' }))
    const editar = screen.getByRole('heading', { name: 'Editar registro' }).closest('section')!
    expect((within(editar).getByLabelText(/Status/) as HTMLSelectElement).value).toBe('available')
  })

  it('morador só consulta as vagas', async () => {
    loginAs('morador')
    await openModule('Estacionamento')

    expect(await screen.findByText('V-01')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'Novo registro' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Editar' })).toBeNull()
  })
})
