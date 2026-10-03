import type { User, UserRole } from '../types/user'

export interface Condominium {
  id: string
  name: string
}

export interface CreateUserPayload {
  condominiumId: string
  unitId?: string
  name: string
  email: string
  password: string
  phone: string
  cpf: string
  role: UserRole
  birthDate?: string
}

const API_BASE = import.meta.env.VITE_API_BASE_URL || (import.meta.env.PROD ? 'https://gestaopro-api-99vx.onrender.com' : '')

export async function fetchAllUsers(): Promise<User[]> {
  const response = await fetch(`${API_BASE}/api/users`)
  if (!response.ok) throw new Error(`Erro ao carregar usuários (${response.status})`)
  return response.json()
}

export async function fetchCondominiums(): Promise<Condominium[]> {
  const response = await fetch(`${API_BASE}/api/condominiums`)
  if (!response.ok) throw new Error(`Erro ao carregar condomínios (${response.status})`)
  return response.json()
}

export async function createUser(data: CreateUserPayload): Promise<User> {
  const response = await fetch(`${API_BASE}/api/users`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  if (!response.ok) {
    const text = await response.text()
    throw new Error(`Erro ao criar usuário (${response.status})${text ? ': ' + text : ''}`)
  }
  return response.json()
}
