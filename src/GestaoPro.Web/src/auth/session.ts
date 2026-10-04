import { AppSession, LoginResponse, ModuleDef } from '../domain/types'

// Sessão do painel guardada no localStorage (o token é o mesmo JWT que a API valida).
const KEYS = {
  token: 'gestaopro_token',
  userId: 'gestaopro_user_id',
  userName: 'gestaopro_user_name',
  role: 'gestaopro_role',
  accessType: 'gestaopro_access_type',
  condominiumId: 'gestaopro_condominium_id',
  registrationNumber: 'gestaopro_registration_number',
} as const

export const adminRoles = ['admin', 'sindico']

export const roleLabels: Record<string, string> = {
  admin: 'Administração',
  sindico: 'Síndico',
  porteiro: 'Portaria',
  morador: 'Morador',
}

export function readSession(): AppSession {
  return {
    token: localStorage.getItem(KEYS.token) ?? '',
    userId: localStorage.getItem(KEYS.userId) ?? '',
    userName: localStorage.getItem(KEYS.userName) ?? '',
    role: localStorage.getItem(KEYS.role) ?? '',
    accessType: localStorage.getItem(KEYS.accessType) ?? '',
    condominiumId: localStorage.getItem(KEYS.condominiumId) ?? '',
    registrationNumber: localStorage.getItem(KEYS.registrationNumber) ?? '',
  }
}

// O papel vem do token (resposta do login): quem administra entra na visão de gestão.
export function storeLogin(response: LoginResponse): AppSession {
  const session: AppSession = {
    token: response.token,
    userId: response.userId,
    userName: response.userName || response.registrationNumber,
    role: response.role,
    accessType: isAdminRole(response.role) ? 'admin' : 'usuario',
    condominiumId: response.condominiumId,
    registrationNumber: response.registrationNumber,
  }
  for (const [key, storageKey] of Object.entries(KEYS)) {
    localStorage.setItem(storageKey, session[key as keyof AppSession])
  }
  return session
}

// "Ver como morador" (síndico): troca só a visão, o token e o papel continuam os mesmos.
export function storeAccessType(accessType: 'admin' | 'usuario') {
  localStorage.setItem(KEYS.accessType, accessType)
}

export function storeUserName(name: string) {
  localStorage.setItem(KEYS.userName, name)
}

export function clearStoredSession() {
  for (const storageKey of Object.values(KEYS)) localStorage.removeItem(storageKey)
}

export function isAdminRole(role: string) {
  return adminRoles.includes(role)
}

export function isFrontDesk(session: AppSession) {
  return session.role === 'porteiro'
}

export function canManageSession(session: AppSession) {
  return session.accessType === 'admin' && isAdminRole(session.role)
}

// Quem não administra só consulta os próprios registros nos módulos que têm rota por usuário.
export function moduleForSession(module: ModuleDef, session: AppSession, canManage: boolean): ModuleDef {
  if (!session.userId) return module
  const userId = encodeURIComponent(session.userId)
  const condominiumId = encodeURIComponent(session.condominiumId)
  if (canManage) {
    // A gestão lista direto as do próprio condomínio (sem escolher condomínio num filtro).
    if (module.id === 'units') return { ...module, listEndpoint: `/api/Units/condominium/${condominiumId}` }
    if (module.id === 'occurrences') return { ...module, listEndpoint: `/api/Occurrences/condominium/${condominiumId}` }
    if (module.id === 'pets') return { ...module, listEndpoint: `/api/Pets/condominium/${condominiumId}` }
    return module
  }
  if (module.id === 'reservations') return { ...module, listEndpoint: `/api/reservations/user/${userId}` }
  // A portaria recebe os visitantes do condomínio: lista completa (a API filtra pelo condomínio do token).
  if (module.id === 'visitors' && !isFrontDesk(session)) return { ...module, listEndpoint: `/api/Visitor/resident/${userId}` }
  if (module.id === 'bills') return { ...module, listEndpoint: `/api/Bills/user/${userId}` }
  if (module.id === 'occurrences') return { ...module, listEndpoint: `/api/Occurrences/user/${userId}` }
  if (module.id === 'pets') return { ...module, listEndpoint: `/api/Pets/owner/${userId}` }
  return module
}
