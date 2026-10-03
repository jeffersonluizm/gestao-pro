export type UserRole = 'admin' | 'sindico' | 'morador' | 'porteiro'

export interface User {
  id: string
  condominiumId: string
  unitId?: string | null
  name: string
  age?: number | null
  email: string
  registrationNumber: string
  phone: string
  cpf: string
  role: UserRole
  isActive: boolean
  mustChangePassword: boolean
  createdAt: string
  birthDate?: string | null
  residencyDuration?: string | null
  memberSince: string
}
