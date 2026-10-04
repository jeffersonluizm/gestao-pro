import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from '../App'

// A API é a fronteira do painel: o fetch é simulado com respostas no formato dos controllers
// e cada chamada fica registrada em `calls`.
export type Call = { method: string; path: string; body?: unknown }
export type Route = (method: string, path: string, body: unknown) => { status?: number; body?: unknown } | unknown | undefined

export const calls: Call[] = []

export const fixtures = {
  users: [
    { id: 'u-maria', name: 'Maria', registrationNumber: 'maria@condominio.com', role: 'morador', isActive: true },
    { id: 'u-joao', name: 'João', registrationNumber: 'joao@condominio.com', role: 'morador', isActive: true },
  ],
  bills: [
    { id: 'b-1', condominiumId: 'c-1', userId: 'u-maria', description: 'Taxa condominial 05/2026', amount: 450.5, dueDate: '2026-05-10T00:00:00Z', status: 'pending', barcode: 'AUTO-1' },
  ],
  payments: [
    { id: 'p-1', billId: 'b-1', paymentDate: '2026-05-08T13:00:00Z', amountPaid: 450.5, paymentMethod: 'pix', receiptUrl: null },
  ],
  parkings: [
    { id: 'v-1', number: 'V-01', type: 'coberta', status: 'available', isShared: false, unitId: null, residentId: null, reservedByUserId: null },
  ],
  announcements: [
    { id: 'a-1', condominiumId: 'c-1', title: 'Manutenção do elevador', content: 'O elevador social para na sexta.', category: 'Manutenção', isActive: true, createdAt: '2026-10-01T12:00:00Z' },
  ],
}

function defaultRoute(method: string, path: string): unknown {
  if (method === 'GET' && path === '/api/Users') return fixtures.users
  if (method === 'GET' && path.startsWith('/api/Bills/user/')) return fixtures.bills.filter(bill => path.endsWith(bill.userId))
  if (method === 'GET' && path === '/api/Payments/bill/b-1') return fixtures.payments
  if (method === 'GET' && path === '/api/Parking') return fixtures.parkings
  if (method === 'GET' && path.startsWith('/api/announcements')) return fixtures.announcements
  if (method === 'POST' && path === '/api/Payments') return { id: 'p-2' }
  if (method === 'GET' && path === '/api/Condominiums') return [{ id: 'c-1', name: 'Residencial Jardim' }]
  return []
}

export function stubFetch(route?: Route) {
  calls.length = 0
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input), 'http://localhost')
    const method = (init?.method ?? 'GET').toUpperCase()
    const path = decodeURIComponent(url.pathname) + (url.search ? decodeURIComponent(url.search) : '')
    const body = init?.body ? JSON.parse(String(init.body)) : undefined
    calls.push({ method, path: decodeURIComponent(url.pathname), body })
    const custom = route?.(method, decodeURIComponent(url.pathname), body)
    const result = custom === undefined ? { body: defaultRoute(method, path.split('?')[0]) } : custom
    const { status = 200, body: payload } = (result && typeof result === 'object' && ('status' in result || 'body' in result))
      ? result as { status?: number; body?: unknown }
      : { body: result }
    return new Response(status === 204 ? null : JSON.stringify(payload ?? null), {
      status,
      headers: { 'Content-Type': 'application/json' },
    })
  }))
}

export type Kind = 'admin' | 'sindico' | 'morador' | 'porteiro'

export function loginAs(kind: Kind) {
  const manager = kind === 'admin' || kind === 'sindico'
  localStorage.setItem('gestaopro_token', 'token-de-teste')
  localStorage.setItem('gestaopro_user_id', manager ? 'u-admin' : kind === 'porteiro' ? 'u-porteiro' : 'u-maria')
  localStorage.setItem('gestaopro_user_name', manager ? 'Admin' : 'Maria')
  localStorage.setItem('gestaopro_role', kind)
  localStorage.setItem('gestaopro_access_type', manager ? 'admin' : 'usuario')
  localStorage.setItem('gestaopro_condominium_id', 'c-1')
  localStorage.setItem('gestaopro_registration_number', `${kind}@condominio.com`)
}

export function renderApp(path = '/painel') {
  const user = userEvent.setup()
  const view = render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
  return { user, ...view }
}
