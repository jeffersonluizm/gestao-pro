import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App, { formatCurrency, moduleForSession, modules } from './App'

// A API é a fronteira do painel: o fetch é simulado com respostas no formato dos controllers
// (BillsController, PaymentsController, ParkingController) e registra cada chamada.
type Call = { method: string; path: string; body?: unknown }

const users = [
  { id: 'u-maria', name: 'Maria', registrationNumber: 'maria@condominio.com' },
  { id: 'u-joao', name: 'João', registrationNumber: 'joao@condominio.com' },
]
const bills = [
  { id: 'b-1', condominiumId: 'c-1', userId: 'u-maria', description: 'Taxa condominial 05/2026', amount: 450.5, dueDate: '2026-05-10T00:00:00Z', status: 'pending', barcode: 'AUTO-1' },
]
const payments = [
  { id: 'p-1', billId: 'b-1', paymentDate: '2026-05-08T13:00:00Z', amountPaid: 450.5, paymentMethod: 'pix', receiptUrl: null },
]
const parkings = [
  { id: 'v-1', number: 'V-01', type: 'coberta', status: 'available', isShared: false, unitId: null, residentId: null, reservedByUserId: null },
]

let calls: Call[] = []

function routeResponse(method: string, path: string): unknown {
  if (method === 'GET' && path === '/api/Users') return users
  if (method === 'GET' && path.startsWith('/api/Bills/user/')) return bills.filter(bill => path.endsWith(bill.userId))
  if (method === 'GET' && path === '/api/Payments/bill/b-1') return payments
  if (method === 'GET' && path === '/api/Parking') return parkings
  if (method === 'POST' && path === '/api/Payments') return { id: 'p-2' }
  return []
}

function stubFetch() {
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input), 'http://localhost')
    const method = (init?.method ?? 'GET').toUpperCase()
    const path = decodeURIComponent(url.pathname)
    calls.push({ method, path, body: init?.body ? JSON.parse(String(init.body)) : undefined })
    return new Response(JSON.stringify(routeResponse(method, path)), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    })
  }))
}

function loginAs(kind: 'admin' | 'morador' | 'porteiro') {
  localStorage.setItem('gestaopro_token', 'token-de-teste')
  localStorage.setItem('gestaopro_user_id', kind === 'admin' ? 'u-admin' : 'u-maria')
  localStorage.setItem('gestaopro_user_name', kind === 'admin' ? 'Admin' : 'Maria')
  localStorage.setItem('gestaopro_role', kind)
  localStorage.setItem('gestaopro_access_type', kind === 'admin' ? 'admin' : 'usuario')
  localStorage.setItem('gestaopro_condominium_id', 'c-1')
  localStorage.setItem('gestaopro_registration_number', `${kind}@condominio.com`)
}

async function openModule(label: string) {
  const user = userEvent.setup()
  render(<App />)
  await user.click(screen.getByRole('button', { name: label }))
  // Espera os lookups (lista de usuários) chegarem antes de interagir com os filtros.
  await waitFor(() => expect(calls.some(call => call.path === '/api/Users')).toBe(true))
  return user
}

beforeEach(() => {
  calls = []
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
})

describe('navegação', () => {
  it.each(['admin', 'morador'] as const)('mostra Cobranças, Pagamentos e Estacionamento para %s', kind => {
    loginAs(kind)
    render(<App />)
    const nav = screen.getByRole('navigation', { name: 'Módulos' })
    for (const label of ['Cobranças', 'Pagamentos', 'Estacionamento']) {
      expect(within(nav).getByRole('button', { name: label })).toBeTruthy()
    }
  })

  // A API devolve 403 para porteiro em /api/Bills e /api/Payments: os módulos nem aparecem.
  it('esconde Cobranças e Pagamentos do porteiro', () => {
    loginAs('porteiro')
    render(<App />)
    const nav = screen.getByRole('navigation', { name: 'Módulos' })
    expect(within(nav).queryByRole('button', { name: 'Cobranças' })).toBeNull()
    expect(within(nav).queryByRole('button', { name: 'Pagamentos' })).toBeNull()
    expect(within(nav).getByRole('button', { name: 'Estacionamento' })).toBeTruthy()
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
