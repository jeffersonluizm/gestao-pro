import type { Reservation } from '../types/reservation'

export interface CreateReservationPayload {
  commonAreaId: string
  userId: string
  condominiumId: string
  date: string
  startTime: string
  endTime: string
  observations?: string
}

const API_BASE = import.meta.env.VITE_API_BASE_URL || (import.meta.env.PROD ? 'https://gestaopro-api-99vx.onrender.com' : '')

export async function fetchReservationsByUser(userId: string): Promise<Reservation[]> {
  const response = await fetch(
    `${API_BASE}/api/reservations/user/${encodeURIComponent(userId)}`
  )

  if (!response.ok) {
    throw new Error(`Erro ao carregar reservas (${response.status})`)
  }

  return response.json()
}

export async function fetchAllReservations(): Promise<Reservation[]> {
  const response = await fetch(
    `${API_BASE}/api/reservations`
  )

  if (!response.ok) {
    throw new Error(`Erro ao carregar reservas (${response.status})`)
  }

  return response.json()
}

export async function createReservation(
  data: CreateReservationPayload
): Promise<Reservation> {
  const response = await fetch(
    `${API_BASE}/api/reservations`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }
  )

  if (!response.ok) {
    const text = await response.text()
    throw new Error(
      `Erro ao criar reserva (${response.status})${text ? ': ' + text : ''}`
    )
  }

  return response.json()
}