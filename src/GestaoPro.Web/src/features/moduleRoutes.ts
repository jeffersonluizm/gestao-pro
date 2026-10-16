import { IconType } from 'react-icons'
import {
  IoBusinessOutline, IoCalendarOutline, IoCarOutline, IoCardOutline, IoCashOutline, IoChatbubbleEllipsesOutline,
  IoCheckboxOutline, IoCubeOutline, IoHomeOutline, IoKeyOutline, IoMegaphoneOutline, IoPawOutline,
  IoPeopleOutline, IoPersonAddOutline, IoIdCardOutline,
} from 'react-icons/io5'
import { AppSession, ModuleDef, ModuleId } from '../domain/types'
import { isAdminRole, isFrontDesk } from '../auth/session'
import { modules } from './modules'

// Público de cada módulo. A API é quem decide (403/404); o painel só não oferece o que vai ser recusado.
type Audience = 'gestao' | 'morador' | 'porteiro'

interface ModuleRoute {
  slug: string
  group: string
  icon: IconType
  audience: Audience[]
}

const routes: Record<ModuleId, ModuleRoute> = {
  announcements: { slug: 'avisos', group: 'Comunicação', icon: IoMegaphoneOutline, audience: ['gestao', 'morador', 'porteiro'] },
  occurrences: { slug: 'ocorrencias', group: 'Comunicação', icon: IoChatbubbleEllipsesOutline, audience: ['gestao', 'morador', 'porteiro'] },
  assembly: { slug: 'assembleias', group: 'Comunicação', icon: IoPeopleOutline, audience: ['gestao'] },
  votes: { slug: 'votos', group: 'Comunicação', icon: IoCheckboxOutline, audience: ['gestao', 'morador', 'porteiro'] },
  visitors: { slug: 'visitantes', group: 'Portaria', icon: IoPersonAddOutline, audience: ['gestao', 'morador', 'porteiro'] },
  deliveries: { slug: 'encomendas', group: 'Portaria', icon: IoCubeOutline, audience: ['gestao', 'morador', 'porteiro'] },
  commonAreas: { slug: 'areas-comuns', group: 'Espaços', icon: IoKeyOutline, audience: ['gestao', 'morador', 'porteiro'] },
  reservations: { slug: 'reservas', group: 'Espaços', icon: IoCalendarOutline, audience: ['gestao', 'morador', 'porteiro'] },
  parking: { slug: 'estacionamento', group: 'Espaços', icon: IoCarOutline, audience: ['gestao', 'morador', 'porteiro'] },
  bills: { slug: 'cobrancas', group: 'Financeiro', icon: IoCashOutline, audience: ['gestao', 'morador'] },
  payments: { slug: 'pagamentos', group: 'Financeiro', icon: IoCardOutline, audience: ['gestao', 'morador'] },
  condominiums: { slug: 'condominios', group: 'Cadastros', icon: IoBusinessOutline, audience: ['gestao'] },
  units: { slug: 'unidades', group: 'Cadastros', icon: IoHomeOutline, audience: ['gestao'] },
  users: { slug: 'usuarios', group: 'Cadastros', icon: IoIdCardOutline, audience: ['gestao'] },
  pets: { slug: 'pets', group: 'Cadastros', icon: IoPawOutline, audience: ['gestao', 'morador'] },
}

export const groupOrder = ['Comunicação', 'Portaria', 'Espaços', 'Financeiro', 'Cadastros']

function audienceOf(session: AppSession, canManage: boolean): Audience {
  if (canManage) return 'gestao'
  return isFrontDesk(session) ? 'porteiro' : 'morador'
}

export function canSeeModule(module: ModuleDef, session: AppSession, canManage: boolean) {
  return routes[module.id].audience.includes(audienceOf(session, canManage))
}

// Quem administra o módulo no painel: a gestão em todos; a portaria também nas encomendas.
export function canManageModule(module: ModuleDef, session: AppSession, canManage: boolean) {
  if (canManage) return true
  return module.id === 'deliveries' && isFrontDesk(session) && !isAdminRole(session.role)
}

export function routeOf(module: ModuleDef) {
  return routes[module.id]
}

export function modulePath(module: ModuleDef) {
  return `/painel/${routes[module.id].slug}`
}

export function moduleBySlug(slug: string) {
  return modules.find(module => routes[module.id].slug === slug)
}

export function visibleModules(session: AppSession, canManage: boolean) {
  return modules.filter(module => canSeeModule(module, session, canManage))
}
