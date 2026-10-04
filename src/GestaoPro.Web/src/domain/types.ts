export type ModuleId =
  | 'condominiums'
  | 'units'
  | 'users'
  | 'commonAreas'
  | 'reservations'
  | 'announcements'
  | 'visitors'
  | 'pets'
  | 'occurrences'
  | 'deliveries'
  | 'bills'
  | 'payments'
  | 'parking'
  | 'assembly'
  | 'votes'

export type FieldType = 'text' | 'email' | 'password' | 'date' | 'time' | 'number' | 'textarea' | 'select' | 'checkbox'
export type LookupKey = 'condominiums' | 'users' | 'units' | 'commonAreas' | 'assemblies' | 'visitors' | 'deliveries'

export interface SelectOption {
  value: string
  label: string
}

export interface FieldDef {
  name: string
  label: string
  type?: FieldType
  required?: boolean
  placeholder?: string
  options?: string[]
  selectOptions?: SelectOption[]
  defaultValue?: string | number | boolean
  disallowPast?: boolean
  step?: string
}

export interface ModuleDef {
  id: ModuleId
  label: string
  title: string
  description: string
  listEndpoint?: string
  createEndpoint?: string
  createHeaders?: (payload: Record<string, unknown>) => Record<string, string>
  fields?: FieldDef[]
  // Formulário de edição quando difere do de criação (ex.: status só existe no PUT).
  editFields?: FieldDef[]
  columns?: string[]
  // A API não tem DELETE para o recurso: esconde o botão Remover.
  noDelete?: boolean
}

export interface AppSession {
  token: string
  userId: string
  userName: string
  role: string
  accessType: string
  condominiumId: string
  registrationNumber: string
}

export type LookupData = Record<LookupKey, SelectOption[]>

// Registro genérico das telas de CRUD guiadas por configuração (features/modules.ts).
export type Row = Record<string, unknown>

// ── Contratos da API usados pelas telas dedicadas ──

export interface LoginResponse {
  token: string
  userId: string
  userName: string
  registrationNumber: string
  role: string
  condominiumId: string
  mustChangePassword: boolean
  expiresAt?: string
}

// Primeiro acesso: a API responde só isso quando a conta precisa trocar a senha.
export interface MustChangePasswordResponse {
  mustChangePassword: true
  userId: string
  message?: string
}

export interface CondominiumSummary {
  id: string
  name: string
}

export interface CheckCpfResponse {
  canProceed: boolean
  needsProfileData: boolean
  message: string
}

export interface SelfRegisterResponse {
  registrationNumber: string
  name: string
}

export interface UserProfile {
  id: string
  name: string
  email?: string
  phone?: string
  role: string
  registrationNumber?: string
  unitId?: string | null
}

export interface Announcement {
  id: string
  condominiumId: string
  title: string
  content: string
  category: string
  isActive: boolean
  createdAt: string
  expiresAt?: string | null
}

export interface Reservation {
  id: string
  userId: string
  commonAreaId: string
  date: string
  startTime: string
  endTime: string
  status: string
}

export interface Delivery {
  id: string
  trackingCode: string
  type: string
  status: string
  residentId?: string
}

export interface Bill {
  id: string
  description: string
  amount: number
  dueDate: string
  status: string
}

export interface Visitor {
  id: string
  fullName: string
  status: string
}

export interface Occurrence {
  id: string
  title: string
  status: string
  createdAt?: string
}
