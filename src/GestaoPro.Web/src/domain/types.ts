export type ModuleId =
  | 'dashboard'
  | 'auth'
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
  adminOnly?: boolean
  // Papéis para os quais a API recusa as leituras do módulo (403): o módulo nem aparece.
  hiddenForRoles?: string[]
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
