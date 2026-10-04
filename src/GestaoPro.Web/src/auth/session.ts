import { AppSession, ModuleDef } from '../domain/types'

export const adminRoles = ['admin', 'sindico']

export function readSession(): AppSession {
  return {
    token: localStorage.getItem('gestaopro_token') ?? '',
    userId: localStorage.getItem('gestaopro_user_id') ?? '',
    userName: localStorage.getItem('gestaopro_user_name') ?? '',
    role: localStorage.getItem('gestaopro_role') ?? '',
    accessType: localStorage.getItem('gestaopro_access_type') ?? '',
    condominiumId: localStorage.getItem('gestaopro_condominium_id') ?? '',
    registrationNumber: localStorage.getItem('gestaopro_registration_number') ?? '',
  }
}

export function isAdminRole(role: string) {
  return adminRoles.includes(role)
}

export function clearStoredSession() {
  localStorage.removeItem('gestaopro_token')
  localStorage.removeItem('gestaopro_user_id')
  localStorage.removeItem('gestaopro_user_name')
  localStorage.removeItem('gestaopro_role')
  localStorage.removeItem('gestaopro_access_type')
  localStorage.removeItem('gestaopro_condominium_id')
  localStorage.removeItem('gestaopro_registration_number')
}

export function isFrontDesk(session: AppSession) {
  return session.role === 'porteiro'
}

// Quem não administra só consulta os próprios registros nos módulos que têm rota por usuário.
export function moduleForSession(module: ModuleDef, session: AppSession, canManage: boolean): ModuleDef {
  if (canManage || !session.userId) return module
  const userId = encodeURIComponent(session.userId)
  if (module.id === 'reservations') return { ...module, listEndpoint: `/api/reservations/user/${userId}` }
  // A portaria recebe os visitantes do condomínio: lista completa (a API filtra pelo condomínio do token).
  if (module.id === 'visitors' && !isFrontDesk(session)) return { ...module, listEndpoint: `/api/Visitor/resident/${userId}` }
  if (module.id === 'bills') return { ...module, listEndpoint: `/api/Bills/user/${userId}` }
  return module
}
