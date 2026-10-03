export type ReservationStatus = 'pendente' | 'aprovada' | 'rejeitada' | 'cancelada'

export interface Reservation {
  id: string
  commonAreaId: string
  userId: string
  condominiumId: string
  date: string        // "yyyy-MM-dd"
  startTime: string   // "HH:mm"
  endTime: string     // "HH:mm"
  status: ReservationStatus
  observations?: string | null
  createdAt: string
  updatedAt: string
}
