import { FormEvent, ReactNode, useEffect, useMemo, useState } from 'react'
import './App.css'
import gestaoProLogo from './assets/gestao-pro-logo.svg'

type ModuleId =
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

type FieldType = 'text' | 'email' | 'password' | 'date' | 'time' | 'number' | 'textarea' | 'select' | 'checkbox'
type LookupKey = 'condominiums' | 'users' | 'units' | 'commonAreas' | 'assemblies' | 'visitors' | 'deliveries'

interface SelectOption {
  value: string
  label: string
}

interface FieldDef {
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

interface ModuleDef {
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

const API_BASE = import.meta.env.VITE_API_BASE_URL || (import.meta.env.PROD ? 'https://gestaopro-api-99vx.onrender.com' : '')
const adminRoles = ['admin', 'sindico']

const createSuccessMessages: Partial<Record<ModuleId, string>> = {
  users: 'Usuário criado com sucesso.',
  assembly: 'Assembleia criada com sucesso.',
  units: 'Unidade criada com sucesso.',
  condominiums: 'Condomínio criado com sucesso.',
  commonAreas: 'Área comum criada com sucesso.',
  visitors: 'Visitante criado com sucesso.',
  pets: 'Pet criado com sucesso.',
  reservations: 'Reserva criada com sucesso.',
  announcements: 'Aviso criado com sucesso.',
  occurrences: 'Ocorrência criada com sucesso.',
  deliveries: 'Encomenda criada com sucesso.',
  bills: 'Cobrança criada com sucesso.',
  payments: 'Pagamento registrado com sucesso.',
  parking: 'Vaga criada com sucesso.',
  votes: 'Voto registrado.',
}

const updateSuccessMessages: Partial<Record<ModuleId, string>> = {
  users: 'Usuário atualizado com sucesso.',
  assembly: 'Assembleia atualizada com sucesso.',
  units: 'Unidade atualizada com sucesso.',
  condominiums: 'Condomínio atualizado com sucesso.',
  commonAreas: 'Área comum atualizada com sucesso.',
  visitors: 'Visitante atualizado com sucesso.',
  pets: 'Pet atualizado com sucesso.',
  reservations: 'Reserva atualizada com sucesso.',
  announcements: 'Aviso atualizado com sucesso.',
  occurrences: 'Ocorrência atualizada com sucesso.',
  deliveries: 'Encomenda atualizada com sucesso.',
  bills: 'Cobrança atualizada com sucesso.',
  payments: 'Pagamento atualizado com sucesso.',
  parking: 'Vaga atualizada com sucesso.',
}

const deleteSuccessMessages: Partial<Record<ModuleId, string>> = {
  users: 'Usuario desativado com sucesso.',
  assembly: 'Assembleia removida com sucesso.',
  units: 'Unidade removida com sucesso.',
  condominiums: 'Condominio removido com sucesso.',
  commonAreas: 'Area comum removida com sucesso.',
  visitors: 'Visitante removido com sucesso.',
  pets: 'Pet removido com sucesso.',
  reservations: 'Reserva removida com sucesso.',
  announcements: 'Aviso removido com sucesso.',
  occurrences: 'Ocorrencia removida com sucesso.',
  deliveries: 'Encomenda removida com sucesso.',
  parking: 'Vaga removida com sucesso.',
}

function deleteConfirmationMessage(moduleId: ModuleId) {
  if (moduleId === 'users') return 'Desativar este usuario?'
  if (moduleId === 'assembly') return 'Remover esta assembleia? Os votos e o historico serao preservados.'
  if (moduleId === 'visitors') return 'Remover este visitante?'
  if (moduleId === 'pets') return 'Deseja realmente remover este pet?'
  return 'Tem certeza que deseja remover este registro?'
}

function isSuccessMessage(message: string) {
  return /sucesso|registrado/i.test(message)
}

const labels: Record<string, string> = {
  id: 'ID',
  name: 'Nome',
  fullName: 'Nome completo',
  registrationNumber: 'E-mail',
  email: 'E-mail',
  phone: 'Telefone',
  role: 'Perfil',
  isActive: 'Ativo',
  mustChangePassword: 'Trocar senha',
  cnpj: 'CNPJ',
  city: 'Cidade',
  state: 'UF',
  residentCount: 'Moradores',
  number: 'Número',
  block: 'Bloco',
  floor: 'Andar',
  type: 'Tipo',
  date: 'Data',
  startTime: 'Início',
  endTime: 'Fim',
  status: 'Status',
  checkInAt: 'Entrada',
  checkOutAt: 'Saída',
  arrivedAt: 'Chegada',
  pickedUpAt: 'Retirada',
  userId: 'Usuário',
  commonAreaId: 'Área comum',
  condominiumId: 'Condomínio',
  capacity: 'Capacidade',
  openTime: 'Abertura',
  closeTime: 'Fechamento',
  title: 'Título',
  content: 'Conteúdo',
  category: 'Categoria',
  createdAt: 'Criado em',
  unitId: 'Unidade',
  unitLabel: 'Unidade',
  reservedByUserId: 'Reservado por',
  document: 'Documento',
  species: 'Espécie',
  breed: 'Raça',
  color: 'Cor',
  vaccination: 'Vacinado',
  priority: 'Prioridade',
  trackingCode: 'Código',
  carrier: 'Transportadora',
  recipientName: 'Destinatário',
  unitNumber: 'Unidade',
  description: 'Descrição',
  residentId: 'Morador',
  scheduledDate: 'Data',
  assemblyId: 'Assembleia',
  topicIndex: 'Pauta',
  topicDescription: 'Descrição da pauta',
  vote: 'Voto',
  voto: 'Voto',
  voteStatus: 'Status da votação',
  amount: 'Valor',
  dueDate: 'Vencimento',
  barcode: 'Código de barras',
  billId: 'Cobrança',
  paymentDate: 'Data do pagamento',
  amountPaid: 'Valor pago',
  paymentMethod: 'Forma de pagamento',
  receiptUrl: 'Comprovante',
  isShared: 'Compartilhada',
}

const statusLabels: Record<string, string> = {
  pending: 'Pendente',
  picked_up: 'Retirado',
  available: 'Disponível',
  occupied: 'Ocupado',
  reserved: 'Reservado',
  'checked-in': 'Entrada registrada',
  'checked-out': 'Saída registrada',
  pendente: 'Pendente',
  aprovada: 'Aprovada',
  rejeitada: 'Rejeitada',
  cancelada: 'Cancelada',
  paid: 'Pago',
  overdue: 'Vencido',
  canceled: 'Cancelado',
  Open: 'Aberta',
  InProgress: 'Em andamento',
  en_andamento: 'Em andamento',
  Concluded: 'Concluída',
  Cancelled: 'Cancelada',
  agendada: 'Agendada',
  encerrada: 'Encerrada',
}

const optionLabels: Record<string, string> = {
  admin: 'Administrador',
  usuario: 'Usuário',
  sindico: 'Síndico',
  morador: 'Usuário',
  porteiro: 'Porteiro',
  apt: 'Apartamento',
  house: 'Casa',
  Dog: 'Cachorro',
  Cat: 'Gato',
  Bird: 'Ave',
  Rabbit: 'Coelho',
  Other: 'Outro',
  encomenda: 'Encomenda',
  lanche: 'Lanche',
  Maintenance: 'Manutenção',
  Complaint: 'Reclamação',
  Suggestion: 'Sugestão',
  Low: 'Baixa',
  Medium: 'Média',
  High: 'Alta',
  Urgent: 'Urgente',
  pix: 'Pix',
  boleto: 'Boleto',
  cartao: 'Cartão',
  sim: 'Sim',
  nao: 'Não',
  abstencao: 'Abstenção',
}

interface AppSession {
  token: string
  userId: string
  userName: string
  role: string
  accessType: string
  condominiumId: string
  registrationNumber: string
}

type LookupData = Record<LookupKey, SelectOption[]>

const emptyLookups: LookupData = {
  condominiums: [],
  users: [],
  units: [],
  commonAreas: [],
  assemblies: [],
  visitors: [],
  deliveries: [],
}

function readSession(): AppSession {
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

function isAdminRole(role: string) {
  return adminRoles.includes(role)
}

function formatOptionLabel(value: string) {
  return statusLabels[value] ?? optionLabels[value] ?? value
}

function fieldsForSession(module: ModuleDef, canManage: boolean) {
  if (module.id === 'reservations' && !canManage) {
    return module.fields?.filter(field => !['userId', 'condominiumId', 'status'].includes(field.name))
  }

  if (module.id === 'visitors' && !canManage) {
    return module.fields?.filter(field => !['residentId', 'condominiumId', 'status'].includes(field.name))
  }

  return module.fields
}

function lookupKeyForField(fieldName: string): LookupKey | undefined {
  if (fieldName === 'condominiumId') return 'condominiums'
  if (fieldName === 'commonAreaId') return 'commonAreas'
  if (['userId', 'residentId', 'ownerId', 'recipientId', 'reservedByUserId'].includes(fieldName)) return 'users'
  if (fieldName === 'unitId') return 'units'
  if (fieldName === 'assemblyId') return 'assemblies'
  return undefined
}

function withLookupOptions(fields: FieldDef[] | undefined, lookups: LookupData) {
  return fields?.map(field => {
    const lookupKey = lookupKeyForField(field.name)
    if (!lookupKey || !lookups[lookupKey].length) return field
    return {
      ...field,
      type: 'select' as FieldType,
      selectOptions: lookups[lookupKey],
      placeholder: undefined,
    }
  })
}

function todayInputValue() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

function rowId(row: Record<string, unknown>) {
  return String(row.id ?? row._id ?? '')
}

function optionFromRow(row: Record<string, unknown>, fallback: string) {
  const id = rowId(row)
  if (!id) return undefined
  const name = String(row.name ?? row.fullName ?? row.title ?? row.description ?? fallback)
  const registration = row.registrationNumber ? ` - matrícula ${row.registrationNumber}` : ''
  const unit = row.number ? ` - ${row.block ? `Bloco ${row.block} / ` : ''}${row.number}` : ''
  return { value: id, label: `${name}${registration}${unit}` }
}


function formatCell(column: string, value: unknown, lookups?: LookupData) {
  const lookupKey = lookupKeyForField(column)
  if (lookupKey && typeof value === 'string') {
    const option = lookups?.[lookupKey].find(item => item.value === value)
    if (option) return option.label
  }
  if (column === 'topics' && Array.isArray(value)) {
    const titles = value.map(topic => String((topic as Record<string, unknown>).title ?? '')).filter(Boolean)
    return titles.length ? titles.join(', ') : '-'
  }
  if (['birthDate', 'dueDate', 'expiresAt', 'scheduledDate', 'date'].includes(column) && typeof value === 'string') {
    return formatDateOnly(value)
  }
  if (['amount', 'amountPaid'].includes(column)) return formatCurrency(value)

  return formatValue(value)
}

export function formatCurrency(value: unknown) {
  if (value === null || value === undefined || value === '' || Number.isNaN(Number(value))) return '-'
  return Number(value).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

// Campos conforme CreateBillDto / UpdateBillDto (BillsController).
const billFields: FieldDef[] = [
  { name: 'condominiumId', label: 'Condomínio', required: true },
  { name: 'userId', label: 'Morador', required: true },
  { name: 'description', label: 'Descrição', placeholder: 'Ex.: Taxa condominial 05/2026', required: true },
  { name: 'amount', label: 'Valor (R$)', type: 'number', step: '0.01', required: true },
  { name: 'dueDate', label: 'Vencimento', type: 'date', required: true },
  { name: 'barcode', label: 'Código de barras', placeholder: 'Gerado automaticamente se vazio' },
]

// Campos conforme CreatePaymentDto / UpdatePaymentDto; o billId vem da cobrança escolhida na tela.
const paymentFields: FieldDef[] = [
  { name: 'amountPaid', label: 'Valor pago (R$)', type: 'number', step: '0.01', required: true },
  { name: 'paymentMethod', label: 'Forma de pagamento', type: 'select', options: ['pix', 'boleto', 'cartao'], required: true },
  { name: 'receiptUrl', label: 'Link do comprovante', placeholder: 'https://...' },
]

// Campos conforme CreateParkingDto; status e ocupação só existem no UpdateParkingDto.
const parkingFields: FieldDef[] = [
  { name: 'condominiumId', label: 'Condomínio' },
  { name: 'number', label: 'Número', placeholder: 'Ex.: V-01', required: true },
  { name: 'type', label: 'Tipo', placeholder: 'Ex.: coberta, descoberta, visitante', required: true },
  { name: 'unitId', label: 'Unidade' },
  { name: 'isShared', label: 'Compartilhada', type: 'checkbox' },
]

export const modules: ModuleDef[] = [
  {
    id: 'dashboard',
    label: 'Painel',
    title: 'Painel',
    adminOnly: true,
    description: 'Resumo operacional do condomínio.',
  },
  {
    id: 'auth',
    label: 'Login',
    title: 'Autenticação',
    description: 'Login e criação de credenciais.',
    fields: [
      { name: 'registrationNumber', label: 'E-mail', type: 'email', required: true },
      { name: 'accessType', label: 'Tipo de acesso', type: 'select', options: ['admin', 'usuario'], defaultValue: 'usuario', required: true },
      { name: 'password', label: 'Senha', type: 'password', required: true },
    ],
  },
  {
    id: 'condominiums',
    label: 'Condomínios',
    title: 'Condomínios',
    description: 'Cadastro e consulta de condomínios.',
    listEndpoint: '/api/Condominiums',
    createEndpoint: '/api/Condominiums',
    adminOnly: true,
    columns: ['name', 'cnpj', 'city', 'state', 'residentCount'],
    fields: [
      { name: 'name', label: 'Nome', required: true },
      { name: 'cnpj', label: 'CNPJ', placeholder: '00.000.000/0000-00', required: true },
      { name: 'street', label: 'Rua', required: true },
      { name: 'number', label: 'Número', required: true },
      { name: 'neighborhood', label: 'Bairro', required: true },
      { name: 'city', label: 'Cidade', required: true },
      { name: 'state', label: 'UF', required: true },
      { name: 'zipCode', label: 'CEP', placeholder: '00000-000', required: true },
      { name: 'rules', label: 'Regras', type: 'textarea' },
      { name: 'regulations', label: 'Regulamento', type: 'textarea' },
    ],
  },
  {
    id: 'users',
    label: 'Usuários',
    title: 'Usuários',
    description: 'Moradores, síndicos, porteiros e administradores.',
    listEndpoint: '/api/Users',
    createEndpoint: '/api/Users',
    adminOnly: true,
    columns: ['name', 'registrationNumber', 'unitId', 'email', 'phone', 'role', 'isActive'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'unitId', label: 'Unidade' },
      { name: 'name', label: 'Nome', required: true },
      { name: 'email', label: 'E-mail', type: 'email', required: true },
      { name: 'password', label: 'Senha', type: 'password', defaultValue: '123456' },
      { name: 'phone', label: 'Telefone', required: true },
      { name: 'cpf', label: 'CPF', placeholder: '000.000.000-00', required: true },
      { name: 'role', label: 'Perfil', type: 'select', options: ['admin', 'morador'], required: true },
      { name: 'birthDate', label: 'Nascimento', placeholder: 'DD/MM/AAAA' },
    ],
  },
  {
    id: 'units',
    label: 'Unidades',
    title: 'Unidades',
    description: 'Apartamentos, casas e vínculos de moradores.',
    createEndpoint: '/api/Units',
    adminOnly: true,
    columns: ['number', 'block', 'floor', 'type', 'residentCount'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'number', label: 'Número', required: true },
      { name: 'block', label: 'Bloco' },
      { name: 'floor', label: 'Andar', type: 'number' },
      { name: 'type', label: 'Tipo', type: 'select', options: ['apt', 'house'], required: true },
    ],
  },
  {
    id: 'commonAreas',
    label: 'Áreas comuns',
    title: 'Áreas comuns',
    description: 'Cadastro de salões, quadras, churrasqueiras e outros espaços reserváveis.',
    listEndpoint: '/api/CommonAreas',
    createEndpoint: '/api/CommonAreas',
    columns: ['name', 'capacity', 'openTime', 'closeTime', 'isActive', 'condominiumId'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'name', label: 'Nome', required: true },
      { name: 'description', label: 'Descrição', type: 'textarea' },
      { name: 'capacity', label: 'Capacidade', type: 'number', required: true },
      { name: 'openTime', label: 'Abertura', type: 'time', defaultValue: '08:00', required: true },
      { name: 'closeTime', label: 'Fechamento', type: 'time', defaultValue: '22:00', required: true },
      { name: 'rules', label: 'Regras', type: 'textarea' },
    ],
  },
  {
    id: 'reservations',
    label: 'Reservas',
    title: 'Reservas',
    description: 'Reservas de áreas comuns.',
    listEndpoint: '/api/reservations',
    createEndpoint: '/api/reservations',
    columns: ['date', 'startTime', 'endTime', 'status', 'userId', 'commonAreaId'],
    fields: [
      { name: 'commonAreaId', label: 'Área comum', required: true },
      { name: 'userId', label: 'Usuário', required: true },
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'date', label: 'Data', type: 'date', required: true, disallowPast: true },
      { name: 'startTime', label: 'Início', type: 'time', required: true },
      { name: 'endTime', label: 'Fim', type: 'time', required: true },
      { name: 'status', label: 'Status', type: 'select', options: ['pendente', 'aprovada', 'rejeitada', 'cancelada'], defaultValue: 'pendente' },
      { name: 'observations', label: 'Observações', type: 'textarea' },
    ],
  },
  {
    id: 'announcements',
    label: 'Avisos',
    title: 'Avisos',
    description: 'Avisos, comunicados e categorias.',
    listEndpoint: '/api/announcements',
    createEndpoint: '/api/announcements',
    columns: ['title', 'content', 'category', 'condominiumId', 'isActive', 'createdAt'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'title', label: 'Título', required: true },
      { name: 'content', label: 'Conteúdo', type: 'textarea', required: true },
      { name: 'category', label: 'Categoria', required: true },
      { name: 'expiresAt', label: 'Expira em', type: 'date' },
    ],
  },
  {
    id: 'visitors',
    label: 'Visitantes',
    title: 'Visitantes',
    description: 'Cadastro, entrada e saída de visitantes.',
    listEndpoint: '/api/Visitor',
    createEndpoint: '/api/Visitor/register',
    createHeaders: payload => ({ 'X-User-Id': String(payload.residentId || '') }),
    columns: ['fullName', 'document', 'phone', 'condominiumId', 'unitId', 'status', 'checkInAt', 'checkOutAt'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'fullName', label: 'Nome completo', required: true },
      { name: 'document', label: 'Documento', required: true },
      { name: 'phone', label: 'Telefone' },
      { name: 'residentId', label: 'Morador', required: true },
      { name: 'unitId', label: 'Unidade', required: true },
      { name: 'reason', label: 'Motivo', required: true },
      { name: 'status', label: 'Status', type: 'select', options: ['checked-in', 'checked-out'], defaultValue: 'checked-in' },
    ],
  },
  {
    id: 'pets',
    label: 'Pets',
    title: 'Pets',
    description: 'Cadastro e controle de pets por unidade.',
    createEndpoint: '/api/Pets',
    adminOnly: true,
    columns: ['name', 'species', 'breed', 'color', 'vaccination', 'isActive'],
    fields: [
      { name: 'ownerId', label: 'Tutor', required: true },
      { name: 'unitId', label: 'Unidade', required: true },
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'name', label: 'Nome', required: true },
      { name: 'species', label: 'Espécie', type: 'select', options: ['Dog', 'Cat', 'Bird', 'Rabbit', 'Other'], required: true },
      { name: 'breed', label: 'Raça' },
      { name: 'color', label: 'Cor' },
      { name: 'vaccination', label: 'Vacinado', type: 'checkbox' },
      { name: 'observations', label: 'Observações', type: 'textarea' },
    ],
  },
  {
    id: 'occurrences',
    label: 'Ocorrências',
    title: 'Ocorrências',
    description: 'Registros de manutenção, reclamações e sugestões.',
    createEndpoint: '/api/Occurrences',
    adminOnly: true,
    createHeaders: payload => ({
      'X-User-Id': String(payload.userId || ''),
      'X-Condominium-Id': String(payload.condominiumId || ''),
    }),
    columns: ['title', 'category', 'status', 'priority', 'userId', 'createdAt'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'userId', label: 'Usuário', required: true },
      { name: 'title', label: 'Título', required: true },
      { name: 'description', label: 'Descrição', type: 'textarea', required: true },
      { name: 'category', label: 'Categoria', type: 'select', options: ['Maintenance', 'Complaint', 'Suggestion'], required: true },
      { name: 'priority', label: 'Prioridade', type: 'select', options: ['Low', 'Medium', 'High', 'Urgent'], required: true },
      { name: 'status', label: 'Status', type: 'select', options: ['Open', 'InProgress', 'Concluded', 'Cancelled'], defaultValue: 'Open' },
    ],
  },
  {
    id: 'deliveries',
    label: 'Encomendas',
    title: 'Encomendas',
    description: 'Encomendas, lanches e notificação.',
    listEndpoint: '/api/Delivery',
    createEndpoint: '/api/Delivery',
    adminOnly: true,
    columns: ['trackingCode', 'type', 'description', 'residentId', 'unitId', 'status', 'arrivedAt', 'pickedUpAt'],
    fields: [
      { name: 'trackingCode', label: 'Código', required: true },
      { name: 'type', label: 'Tipo', type: 'select', options: ['encomenda', 'lanche'], defaultValue: 'encomenda', required: true },
      { name: 'description', label: 'Descrição / lanche' },
      { name: 'residentId', label: 'Morador', required: true },
      { name: 'unitId', label: 'Unidade' },
      { name: 'status', label: 'Status', type: 'select', options: ['pending', 'picked_up'], defaultValue: 'pending' },
      { name: 'observations', label: 'Observações', type: 'textarea' },
    ],
  },
  {
    id: 'bills',
    label: 'Cobranças',
    title: 'Cobranças',
    description: 'Boletos e taxas por morador.',
    createEndpoint: '/api/Bills',
    hiddenForRoles: ['porteiro'],
    noDelete: true,
    columns: ['description', 'amount', 'dueDate', 'status', 'barcode', 'userId'],
    fields: billFields,
    editFields: [
      ...billFields,
      { name: 'status', label: 'Status', type: 'select', options: ['pending', 'paid', 'overdue', 'canceled'], required: true },
    ],
  },
  {
    id: 'payments',
    label: 'Pagamentos',
    title: 'Pagamentos',
    description: 'Pagamentos registrados por cobrança.',
    createEndpoint: '/api/Payments',
    hiddenForRoles: ['porteiro'],
    noDelete: true,
    columns: ['paymentDate', 'amountPaid', 'paymentMethod', 'receiptUrl'],
    fields: paymentFields,
  },
  {
    id: 'parking',
    label: 'Estacionamento',
    title: 'Estacionamento',
    description: 'Vagas de estacionamento, ocupação e reservas.',
    listEndpoint: '/api/Parking',
    createEndpoint: '/api/Parking',
    columns: ['number', 'type', 'status', 'isShared', 'unitId', 'residentId', 'reservedByUserId'],
    fields: parkingFields,
    editFields: [
      ...parkingFields,
      { name: 'status', label: 'Status', type: 'select', options: ['available', 'reserved', 'occupied'], required: true },
      { name: 'residentId', label: 'Morador' },
      { name: 'reservedByUserId', label: 'Reservado por' },
    ],
  },
  {
    id: 'assembly',
    label: 'Assembleias',
    title: 'Assembleias',
    description: 'Assembleias e pautas.',
    listEndpoint: '/api/assembly',
    createEndpoint: '/api/assembly',
    adminOnly: true,
    columns: ['title', 'description', 'startTime', 'endTime', 'voteStatus', 'topics'],
    fields: [
      { name: 'condominiumId', label: 'Condomínio', required: true },
      { name: 'title', label: 'Título', required: true },
      { name: 'description', label: 'Descrição', type: 'textarea', required: true },
      { name: 'startDate', label: 'Data início', type: 'date', required: true },
      { name: 'endDate', label: 'Data fim', type: 'date', required: true },
      { name: 'topics', label: 'Pautas da assembleia', type: 'textarea', placeholder: 'Uma pauta por linha', required: true },
    ],
  },
  {
    id: 'votes',
    label: 'Votos',
    title: 'Votos',
    description: 'Consulta e envio de votos.',
    createEndpoint: '/api/vote',
    columns: ['topicDescription', 'topicIndex', 'userId', 'voto', 'createdAt'],
    fields: [
      { name: 'assemblyId', label: 'Assembleia', required: true },
      { name: 'topicIndex', label: 'Pauta', type: 'number', required: true },
      { name: 'userId', label: 'Usuário', required: true },
      { name: 'vote', label: 'Voto', type: 'select', options: ['sim', 'não'], required: true },
    ],
  },
]

function endpoint(path: string) {
  return `${API_BASE}${path}`
}

const SESSION_EXPIRED_EVENT = 'gestaopro:session-expired'

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const token = localStorage.getItem('gestaopro_token')
  if (token && !headers.has('Authorization')) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(endpoint(path), {
    ...init,
    headers,
  })

  if (!response.ok) {
    const text = await response.text()
    if (response.status === 401) {
      clearStoredSession()
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
      throw new Error('Sessão expirada ou login obrigatório. Saia e entre novamente.')
    }
    throw new Error(formatApiError(text, response.status))
  }

  if (response.status === 204) return undefined as T
  return response.json()
}

function clearStoredSession() {
  localStorage.removeItem('gestaopro_token')
  localStorage.removeItem('gestaopro_user_id')
  localStorage.removeItem('gestaopro_user_name')
  localStorage.removeItem('gestaopro_role')
  localStorage.removeItem('gestaopro_access_type')
  localStorage.removeItem('gestaopro_condominium_id')
  localStorage.removeItem('gestaopro_registration_number')
}

function isRegistrationLike(value: string) {
  return /^COND-\d{4}-\d{4}$/i.test(value.trim())
}

async function resolveLoggedUserName(userId: string, fallback: string) {
  if (!userId) return fallback
  try {
    const user = await requestJson<Record<string, unknown>>(`/api/Users/${encodeURIComponent(userId)}`)
    return String(user.name ?? fallback)
  } catch {
    return fallback
  }
}

function formatApiError(text: string, status: number) {
  if (status === 403) return 'Você não tem permissão para realizar esta ação.'
  if (status === 404) return 'Registro não encontrado.'
  if (!text) return 'Não foi possível concluir a operação. Tente novamente.'

  try {
    const data = JSON.parse(text) as { message?: string; title?: string; errors?: Record<string, string[]> }
    if (data.message) return data.message
    if (data.errors) {
      if (Object.keys(data.errors).some(key => key.toLowerCase().includes('birthdate'))) {
        return 'Data de nascimento inválida. Informe a data no formato DD/MM/AAAA.'
      }
      return 'Verifique os dados informados.'
    }
    if (data.title && !data.title.toLowerCase().includes('validation errors')) return data.title
  } catch {
    if (text.length < 180 && !text.includes('<') && !text.includes(' at ')) return text
  }

  return status >= 500
    ? 'Ocorreu um erro inesperado. Tente novamente.'
    : 'Não foi possível concluir a operação. Verifique os dados informados.'
}

function normalizeId(row: Record<string, unknown>) {
  const mongoId = typeof row._id === 'object' && row._id !== null
    ? (row._id as Record<string, unknown>).$oid
    : row._id
  return String(row.id ?? mongoId ?? row.userId ?? row.usuarioId ?? row.visitorId ?? '').trim()
}

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === '') return '-'
  if (typeof value === 'string' && (statusLabels[value] || optionLabels[value])) return formatOptionLabel(value)
  if (typeof value === 'boolean') return value ? 'Sim' : 'Não'
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) return new Date(value).toLocaleString('pt-BR')
  if (Array.isArray(value)) return `${value.length} item(ns)`
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

function formatDateOnly(value: string) {
  if (!value) return '-'
  const datePart = value.slice(0, 10)
  const [year, month, day] = datePart.split('-')
  if (!year || !month || !day) return formatValue(value)
  return `${day}/${month}/${year}`
}

function onlyDigits(value: string) {
  return value.replace(/\D/g, '')
}

function maskCnpj(value: string) {
  return onlyDigits(value)
    .slice(0, 14)
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2')
}

function maskCpf(value: string) {
  return onlyDigits(value)
    .slice(0, 11)
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1-$2')
}

function isValidCpfFormat(value: string) {
  return /^\d{3}\.\d{3}\.\d{3}-\d{2}$/.test(value) && onlyDigits(value).length === 11
}

function maskCep(value: string) {
  return onlyDigits(value).slice(0, 8).replace(/^(\d{5})(\d)/, '$1-$2')
}

function maskDateBR(value: string) {
  const digits = onlyDigits(value).slice(0, 8)
  if (digits.length <= 2) return digits
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`
}

function maskPhoneBR(value: string) {
  const digits = onlyDigits(value).slice(0, 10)
  if (digits.length <= 2) return digits
  if (digits.length <= 6) return `${digits.slice(0, 2)} ${digits.slice(2)}`
  return `${digits.slice(0, 2)} ${digits.slice(2, 6)}-${digits.slice(6)}`
}

function maskTime(value: string) {
  const digits = onlyDigits(value).slice(0, 4)
  if (digits.length <= 2) return digits
  return `${digits.slice(0, 2)}:${digits.slice(2)}`
}

function isValidTime(value: string) {
  const match = /^(\d{2}):(\d{2})$/.exec(value)
  if (!match) return false
  const [, hours, minutes] = match
  return Number(hours) >= 0 && Number(hours) <= 23
    && Number(minutes) >= 0 && Number(minutes) <= 59
}

function dateBRToApi(value: string) {
  const match = value.trim().match(/^(\d{2})\/(\d{2})\/(\d{4})$/)
  if (!match) return null
  const [, day, month, year] = match
  const date = new Date(Number(year), Number(month) - 1, Number(day))
  if (date.getFullYear() !== Number(year) || date.getMonth() + 1 !== Number(month) || date.getDate() !== Number(day)) return null
  return `${year}-${month}-${day}`
}

function apiDateToBR(value: string) {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value
}

function formatBirthDate(value: string) {
  return apiDateToBR(value)
}

function parseBirthDate(value: string) {
  return dateBRToApi(value)
}

function formatDateTime(value: string | undefined) {
  if (!value) return '-'

  const text = String(value).trim()
  const slashMatch = text.match(/^(\d{2})\/(\d{2})\/(\d{4})/)
  if (slashMatch) return `${slashMatch[1]}/${slashMatch[2]}/${slashMatch[3]}`

  const isoMatch = text.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (isoMatch) return `${isoMatch[3]}/${isoMatch[2]}/${isoMatch[1]}`

  const date = new Date(text)
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleDateString('pt-BR')
}

function dateInputToIso(value: unknown, endOfDay = false) {
  const match = String(value ?? '').match(/^(\d{4})-(\d{2})-(\d{2})$/)
  if (!match) return value
  const [, year, month, day] = match
  return new Date(
    Number(year),
    Number(month) - 1,
    Number(day),
    endOfDay ? 23 : 0,
    endOfDay ? 59 : 0,
    endOfDay ? 59 : 0,
    endOfDay ? 999 : 0
  ).toISOString()
}

function parseTopicsInput(value: string) {
  const trimmed = value.trim()
  if (!trimmed) return []

  try {
    const parsed = JSON.parse(trimmed)
    if (Array.isArray(parsed)) {
      return parsed.map(item => ({
        title: String((item as Record<string, unknown>).title ?? ''),
        desc: String((item as Record<string, unknown>).desc ?? ''),
        voteOptions: Array.isArray((item as Record<string, unknown>).voteOptions)
          ? ((item as Record<string, unknown>).voteOptions as string[]).map(option => String(option))
          : ['sim', 'não'],
      })).filter(topic => topic.title.trim())
    }
  } catch {
    // fallback para texto simples
  }

  return trimmed
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)
    .map(title => ({ title, desc: '', voteOptions: ['sim', 'não'] }))
}

function fieldInitialValue(field: FieldDef) {
  if (field.defaultValue !== undefined) return field.defaultValue
  if (field.type === 'checkbox') return false
  return ''
}

function buildPayload(fields: FieldDef[], data: Record<string, unknown>) {
  const payload: Record<string, unknown> = {}
  for (const field of fields) {
    const value = data[field.name]
    if ((value === '' || value === undefined) && !field.required) continue
    if (field.name === 'topics' && typeof value === 'string') {
      const parsed = parseTopicsInput(value)
      if (!parsed.length) continue
      payload[field.name] = parsed
      continue
    }
    if (field.name === 'birthDate' && typeof value === 'string') {
      const birthDate = parseBirthDate(value)
      if (!birthDate) throw new Error('Informe uma data válida no formato DD/MM/AAAA.')
      payload[field.name] = birthDate
      continue
    }
    if (field.name === 'cnpj' && typeof value === 'string') {
      if (onlyDigits(value).length !== 14) throw new Error('Informe um CNPJ válido no formato 00.000.000/0000-00.')
      payload[field.name] = maskCnpj(value)
      continue
    }
    if (field.name === 'cpf' && typeof value === 'string') {
      if (!isValidCpfFormat(value)) throw new Error('Informe um CPF válido no formato 000.000.000-00.')
      payload[field.name] = maskCpf(value)
      continue
    }
    if (field.name === 'zipCode' && typeof value === 'string') {
      if (onlyDigits(value).length !== 8) throw new Error('Informe um CEP válido no formato 00000-000.')
      payload[field.name] = maskCep(value)
      continue
    }
    if (field.name === 'phone' && typeof value === 'string') {
      if (onlyDigits(value).length !== 10) throw new Error('Informe um telefone válido no formato 31 9999-2702.')
      payload[field.name] = maskPhoneBR(value)
      continue
    }
    if (field.type === 'time' && typeof value === 'string') {
      if (!isValidTime(value)) throw new Error('Informe uma hora válida no formato HH:MM.')
      payload[field.name] = maskTime(value)
      continue
    }
    if (field.type === 'date' && typeof value === 'string') {
      const apiDate = dateBRToApi(value)
      if (!apiDate) throw new Error('Informe uma data válida no formato DD/MM/AAAA.')
      payload[field.name] = apiDate
      continue
    }
    if (field.type === 'number') {
      payload[field.name] = value === '' || value === undefined ? null : Number(value)
    } else {
      payload[field.name] = value
    }
  }
  return payload
}

function formInitialValue(field: FieldDef, initialData?: Record<string, unknown>) {
  const rawValue = initialData?.[field.name]
  const fallbackValue = field.name === 'startDate' ? initialData?.['startTime'] : field.name === 'endDate' ? initialData?.['endTime'] : undefined
  const value = rawValue ?? fallbackValue
  if (value === undefined || value === null || value === '') return fieldInitialValue(field)
  if (field.name === 'birthDate' && typeof value === 'string') return formatBirthDate(value)
  if (field.name === 'cnpj' && typeof value === 'string') return maskCnpj(value)
  if (field.name === 'cpf' && typeof value === 'string') return maskCpf(value)
  if (field.name === 'zipCode' && typeof value === 'string') return maskCep(value)
  if (field.name === 'phone' && typeof value === 'string') return maskPhoneBR(value)
  if (field.type === 'time' && typeof value === 'string') return maskTime(value)
  if (field.type === 'date' && typeof value === 'string') return apiDateToBR(value)
  if (field.name === 'topics' && Array.isArray(value)) return JSON.stringify(value, null, 2)
  if (typeof value === 'object' && !Array.isArray(value)) {
    const id = normalizeId(value as Record<string, unknown>)
    return resolveSelectValue(field, id) || id || fieldInitialValue(field)
  }
  return resolveSelectValue(field, String(value)) || value
}

function normalizeOptionText(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .trim()
}

function resolveSelectValue(field: FieldDef, value: string) {
  const options = field.selectOptions ?? (field.options ?? []).map(option => ({ value: option, label: formatOptionLabel(option) }))
  if (!options.length || !value) return ''
  const normalizedValue = normalizeOptionText(value)
  const exact = options.find(option => String(option.value) === value)
  if (exact) return exact.value
  const byText = options.find(option =>
    normalizeOptionText(option.label) === normalizedValue ||
    normalizeOptionText(String(option.value)) === normalizedValue
  )
  if (byText) return byText.value
  const byContainedText = options.find(option => {
    const label = normalizeOptionText(option.label)
    return label.includes(normalizedValue) || normalizedValue.includes(label)
  })
  return byContainedText?.value ?? ''
}

function isFrontDesk(session: AppSession) {
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

function filterRowsByMonth(rows: Record<string, unknown>[], month: Date, fieldName: string) {
  const monthKey = `${month.getFullYear()}-${String(month.getMonth() + 1).padStart(2, '0')}`
  return rows.filter(row => String(row[fieldName] ?? '').slice(0, 7) === monthKey)
}

export default function App() {
  const [activeId, setActiveId] = useState<ModuleId>('dashboard')
  const [session, setSession] = useState<AppSession>(() => readSession())
  const [showRegister, setShowRegister] = useState(false)
  const [lookups, setLookups] = useState<LookupData>(emptyLookups)
  const isLoggedIn = Boolean(session.token)
  const canManage = session.accessType === 'admin' && isAdminRole(session.role)
  const canSeeModule = (module: ModuleDef) =>
    (!module.adminOnly || canManage) && !(module.hiddenForRoles ?? []).includes(session.role)
  const visibleModules = modules.filter(module => module.id !== 'auth' && canSeeModule(module))
  const selectedModule = modules.find(module => module.id === activeId) ?? modules[0]
  const fallbackModule = visibleModules[0] ?? modules.find(module => module.id === 'auth')!
  const activeModule = !isLoggedIn ? modules.find(module => module.id === 'auth')! : !canSeeModule(selectedModule) ? fallbackModule : selectedModule
  const pageModule = moduleForSession(activeModule, session, canManage)

  async function refreshLookups(currentSession = session) {
    if (!currentSession.token) {
      setLookups(emptyLookups)
      return
    }

    const readList = async (path: string) => {
      try {
        const data = await requestJson<Record<string, unknown>[]>(path)
        return Array.isArray(data) ? data : []
      } catch {
        return []
      }
    }

    const [condominiums, users, commonAreas, assemblies, units, visitors, deliveries] = await Promise.all([
      readList('/api/Condominiums'),
      readList('/api/Users'),
      readList('/api/CommonAreas'),
      readList('/api/assembly'),
      currentSession.condominiumId ? readList(`/api/Units/condominium/${encodeURIComponent(currentSession.condominiumId)}`) : Promise.resolve([]),
      readList('/api/Visitor'),
      readList('/api/Delivery'),
    ])

    setLookups({
      condominiums: condominiums.map((row, index) => optionFromRow(row, `Condomínio ${index + 1}`)).filter(Boolean) as SelectOption[],
      users: users.map((row, index) => optionFromRow(row, `Usuário ${index + 1}`)).filter(Boolean) as SelectOption[],
      units: units.map((row, index) => optionFromRow(row, `Unidade ${index + 1}`)).filter(Boolean) as SelectOption[],
      commonAreas: commonAreas.map((row, index) => optionFromRow(row, `Área comum ${index + 1}`)).filter(Boolean) as SelectOption[],
      assemblies: assemblies.map((row, index) => optionFromRow(row, `Assembleia ${index + 1}`)).filter(Boolean) as SelectOption[],
      visitors: visitors.map((row, index) => optionFromRow(row, `Visitante ${index + 1}`)).filter(Boolean) as SelectOption[],
      deliveries: deliveries.map((row, index) => optionFromRow(row, `Encomenda ${index + 1}`)).filter(Boolean) as SelectOption[],
    })
  }

  useEffect(() => {
    refreshLookups()
  }, [session.token, session.condominiumId])

  useEffect(() => {
    if (!session.token || !session.userId) return
    if (session.userName && !isRegistrationLike(session.userName)) return

    resolveLoggedUserName(session.userId, session.registrationNumber).then(name => {
      if (!name || name === session.userName || isRegistrationLike(name)) return
      localStorage.setItem('gestaopro_user_name', name)
      setSession(readSession())
    })
  }, [session.token, session.userId, session.userName, session.registrationNumber])

  function updateSession() {
    const nextSession = readSession()
    const nextCanManage = nextSession.accessType === 'admin' && isAdminRole(nextSession.role)
    setSession(nextSession)
    setActiveId(nextCanManage ? 'dashboard' : 'announcements')
    refreshLookups(nextSession)
  }

  function logout() {
    localStorage.removeItem('gestaopro_token')
    localStorage.removeItem('gestaopro_user_id')
    localStorage.removeItem('gestaopro_user_name')
    localStorage.removeItem('gestaopro_role')
    localStorage.removeItem('gestaopro_access_type')
    localStorage.removeItem('gestaopro_condominium_id')
    localStorage.removeItem('gestaopro_registration_number')
    setSession(readSession())
    setLookups(emptyLookups)
    setActiveId('auth')
  }

  // A API devolveu 401 (token expirado): volta para a tela de login em vez de manter o painel aberto.
  useEffect(() => {
    window.addEventListener(SESSION_EXPIRED_EVENT, logout)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, logout)
  }, [])

  if (!isLoggedIn) {
    return (
      <main className="login-shell">
        <div className="login-brand">
          <img className="brand-logo" src={gestaoProLogo} alt="Gestão Pro" />
          <div>
            <strong>Gestão Pro</strong>
            <span>Eficiência para seu condomínio</span>
          </div>
        </div>
        {showRegister
          ? <RegisterPage onBack={() => setShowRegister(false)} />
          : <AuthPage onSessionChange={updateSession} onRegister={() => setShowRegister(true)} />
        }
      </main>
    )
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <img className="brand-logo" src={gestaoProLogo} alt="Gestão Pro" />
          <div>
            <strong>Gestão Pro</strong>
            <span>Condomínio</span>
          </div>
        </div>
        <nav className="nav-list" aria-label="Módulos">
          {visibleModules.map(module => (
            <button
              key={module.id}
              className={activeId === module.id ? 'nav-item active' : 'nav-item'}
              onClick={() => setActiveId(module.id)}
              type="button"
            >
              {module.label}
            </button>
          ))}
        </nav>
      </aside>

      <main className="workspace">
        <header className="page-heading">
          <div>
            <h1>{pageModule.title}</h1>
            <p>{pageModule.description}</p>
          </div>
          <div className="header-actions">
            <span className="hello-user">Olá {session.userName || session.registrationNumber}</span>
            <span className={canManage ? 'access-badge admin' : 'access-badge user'}>
              {canManage ? 'Administrador' : 'Usuário'}
            </span>
            <button type="button" className="secondary-button" onClick={logout}>Sair</button>
          </div>
        </header>

        {activeModule.id === 'dashboard' && <Dashboard />}
        {activeModule.id === 'auth' && <AuthPage onSessionChange={updateSession} />}
        {activeModule.id === 'units' && <UnitsPage module={pageModule} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'pets' && <FilteredPage module={pageModule} filters={petFilters} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'occurrences' && <FilteredPage module={pageModule} filters={occurrenceFilters} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'votes' && <VotesPage module={pageModule} canManage={canManage} session={session} lookups={lookups} />}
        {activeModule.id === 'bills' && <FilteredPage module={pageModule} filters={canManage ? billFilters : null} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'payments' && <PaymentsPage module={pageModule} canManage={canManage} session={session} lookups={lookups} />}
        {!['dashboard', 'auth', 'units', 'pets', 'occurrences', 'votes', 'bills', 'payments'].includes(activeModule.id) && (
          <ResourcePage
            key={activeModule.id}
            module={pageModule}
            canManage={canManage}
            session={session}
            lookups={lookups}
            onChanged={activeModule.id === 'assembly' ? () => refreshLookups() : undefined}
          />
        )}
      </main>
    </div>
  )
}

function Dashboard() {
  const probes = [
    { label: 'Usuários', path: '/api/Users' },
    { label: 'Áreas comuns', path: '/api/CommonAreas' },
    { label: 'Reservas', path: '/api/reservations' },
    { label: 'Avisos', path: '/api/announcements' },
    { label: 'Visitantes', path: '/api/Visitor' },
    { label: 'Pacotes', path: '/api/Package' },
    { label: 'Encomendas', path: '/api/Delivery' },
    { label: 'Assembleias', path: '/api/assembly' },
  ]
  const [counts, setCounts] = useState<Record<string, number | string>>({})

  useEffect(() => {
    Promise.all(
      probes.map(async item => {
        try {
          const data = await requestJson<unknown[]>(item.path)
          return [item.label, Array.isArray(data) ? data.length : 0] as const
        } catch {
          return [item.label, 'sem conexão'] as const
        }
      })
    ).then(results => setCounts(Object.fromEntries(results)))
  }, [])

  return (
    <section className="dashboard-grid">
      {probes.map(item => (
        <article className="metric-card" key={item.label}>
          <span>{item.label}</span>
          <strong>{counts[item.label] ?? '-'}</strong>
        </article>
      ))}
    </section>
  )
}

function AuthPage({ onSessionChange, onRegister }: { onSessionChange: () => void; onRegister?: () => void }) {
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [pendingPasswordUserId, setPendingPasswordUserId] = useState('')
  const changePasswordFields: FieldDef[] = [
    { name: 'currentPassword', label: 'Senha atual', type: 'password', required: true },
    { name: 'newPassword', label: 'Nova senha', type: 'password', required: true },
  ]

  async function handleLogin(payload: Record<string, unknown>) {
    setLoading(true)
    setMessage('')
    try {
      const requestedAccess = String(payload.accessType || 'usuario')
      const credentials = { ...payload }
      delete credentials.accessType
      const response = await requestJson<Record<string, unknown>>('/api/Auth/login', {
        method: 'POST',
        body: JSON.stringify(credentials),
      })
      if (response.mustChangePassword && response.userId) {
        setPendingPasswordUserId(String(response.userId))
        setMessage('Primeiro acesso: troque a senha para continuar.')
        return
      }
      const responseRole = String(response.role ?? '')
      if (requestedAccess === 'admin' && !isAdminRole(responseRole)) {
        localStorage.removeItem('gestaopro_token')
        localStorage.removeItem('gestaopro_user_id')
        localStorage.removeItem('gestaopro_user_name')
        localStorage.removeItem('gestaopro_role')
        localStorage.removeItem('gestaopro_access_type')
        localStorage.removeItem('gestaopro_condominium_id')
        localStorage.removeItem('gestaopro_registration_number')
        setMessage('Essa matrícula não possui acesso de administrador.')
        return
      }
      if (response.token) localStorage.setItem('gestaopro_token', String(response.token))
      if (response.userId) localStorage.setItem('gestaopro_user_id', String(response.userId))
      if (response.role) localStorage.setItem('gestaopro_role', String(response.role))
      localStorage.setItem('gestaopro_access_type', requestedAccess)
      if (response.condominiumId) localStorage.setItem('gestaopro_condominium_id', String(response.condominiumId))
      if (response.registrationNumber) localStorage.setItem('gestaopro_registration_number', String(response.registrationNumber))
      const fallbackName = String(response.userName || response.registrationNumber || '')
      const userName = await resolveLoggedUserName(String(response.userId || ''), fallbackName)
      if (userName) localStorage.setItem('gestaopro_user_name', userName)
      onSessionChange()
      setPendingPasswordUserId('')
      setMessage(`Login realizado como ${formatOptionLabel(requestedAccess)}.`)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao autenticar.')
    } finally {
      setLoading(false)
    }
  }

  async function handleChangePassword(payload: Record<string, unknown>) {
    if (!pendingPasswordUserId) return
    await requestJson('/api/Auth/change-password', {
      method: 'PATCH',
      body: JSON.stringify({ userId: pendingPasswordUserId, ...payload }),
    })
    setPendingPasswordUserId('')
    setMessage('Senha alterada. Faça login novamente com a nova senha.')
  }

  return (
    <div className="panel-grid two">
      <JsonForm title="Login" fields={modules.find(m => m.id === 'auth')?.fields ?? []} onSubmit={handleLogin} submitting={loading} submitLabel="Entrar" successMessage="" />
      {pendingPasswordUserId && <JsonForm title="Trocar senha" fields={changePasswordFields} onSubmit={handleChangePassword} />}
      {message && <div className="notice error span-two">{message}</div>}
      {onRegister && !pendingPasswordUserId && (
        <div className="notice span-two" style={{ textAlign: 'center' }}>
          Primeira vez no sistema?{' '}
          <button type="button" className="link-button" onClick={onRegister}>Cadastrar-se</button>
        </div>
      )}
    </div>
  )
}

function RegisterPage({ onBack }: { onBack: () => void }) {
  const [condominiums, setCondominiums] = useState<SelectOption[]>([])
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [registrationNumber, setRegistrationNumber] = useState('')
  const [registeredName, setRegisteredName] = useState('')
  const [form, setForm] = useState({ condominiumId: '', cpf: '', condominiumCode: '', password: '', confirmPassword: '' })

  useEffect(() => {
    fetch(`${API_BASE}/api/Condominiums`)
      .then(r => r.ok ? r.json() : Promise.reject())
      .then((data: Record<string, unknown>[]) => {
        setCondominiums(
          data
            .map(c => ({ value: String(c.id ?? ''), label: String(c.name ?? '') }))
            .filter(c => c.value)
        )
      })
      .catch(() => setMessage('Não foi possível carregar a lista de condomínios. Verifique se a API está rodando.'))
  }, [])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!form.condominiumId) { setMessage('Selecione um condomínio.'); return }
    if (!isValidCpfFormat(form.cpf)) { setMessage('Informe um CPF válido no formato 000.000.000-00.'); return }
    if (!form.condominiumCode.trim()) { setMessage('Informe o código do condomínio.'); return }
    if (form.password !== form.confirmPassword) { setMessage('As senhas não coincidem.'); return }
    if (form.password.length < 6) { setMessage('A senha deve ter pelo menos 6 caracteres.'); return }
    setLoading(true)
    setMessage('')
    try {
      const result = await requestJson<{ registrationNumber: string; name: string }>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ cpf: onlyDigits(form.cpf), condominiumId: form.condominiumId, condominiumCode: form.condominiumCode.trim(), password: form.password }),
      })
      setRegistrationNumber(result.registrationNumber)
      setRegisteredName(result.name)
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Erro ao cadastrar.')
    } finally {
      setLoading(false)
    }
  }

  if (registrationNumber) {
    return (
      <div className="panel-grid">
        <section className="panel">
          <h2>Cadastro realizado!</h2>
          <p>Olá, <strong>{registeredName}</strong>. Seu acesso foi criado com sucesso.</p>
          <p style={{ margin: '1rem 0 0.25rem' }}>Faça login com o seu e-mail:</p>
          <p style={{ fontSize: '1.2rem', fontWeight: 700, margin: '0 0 1rem' }}>{registrationNumber}</p>
          <p style={{ marginBottom: '1.5rem', color: 'var(--text-muted, #666)' }}>
            Use seu e-mail e a senha que você acabou de definir para entrar.
          </p>
          <button type="button" onClick={onBack}>Ir para Login</button>
        </section>
      </div>
    )
  }

  return (
    <div className="panel-grid">
      <section className="panel">
        <h2>Cadastrar-se</h2>
        <p style={{ marginBottom: '1rem', color: 'var(--text-muted, #666)' }}>
          Seu CPF precisa estar pré-cadastrado pelo administrador do condomínio, que também informa o código do condomínio.
        </p>
        <form onSubmit={handleSubmit} className="form-grid">
          <label>
            Condomínio
            <select
              value={form.condominiumId}
              onChange={e => setForm(f => ({ ...f, condominiumId: e.target.value }))}
              required
            >
              <option value="">Selecione...</option>
              {condominiums.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
            </select>
          </label>
          <label>
            CPF
            <input
              type="text"
              inputMode="numeric"
              maxLength={14}
              placeholder="000.000.000-00"
              value={form.cpf}
              onChange={e => setForm(f => ({ ...f, cpf: maskCpf(e.target.value) }))}
              required
            />
          </label>
          <label>
            Código do condomínio
            <input
              type="password"
              autoComplete="off"
              value={form.condominiumCode}
              onChange={e => setForm(f => ({ ...f, condominiumCode: e.target.value }))}
              required
            />
          </label>
          <label>
            Senha
            <input
              type="password"
              placeholder="Mínimo 6 caracteres"
              value={form.password}
              onChange={e => setForm(f => ({ ...f, password: e.target.value }))}
              required
            />
          </label>
          <label>
            Confirmar senha
            <input
              type="password"
              value={form.confirmPassword}
              onChange={e => setForm(f => ({ ...f, confirmPassword: e.target.value }))}
              required
            />
          </label>
          {message && <div className="notice error">{message}</div>}
          <div className="form-actions">
            <button type="submit" disabled={loading}>{loading ? 'Cadastrando...' : 'Cadastrar'}</button>
            <button type="button" className="secondary-button" onClick={onBack}>Voltar ao Login</button>
          </div>
        </form>
      </section>
    </div>
  )
}

function ResourcePage({
  module,
  canManage,
  session,
  lookups,
  onChanged,
}: {
  module: ModuleDef
  canManage: boolean
  session: AppSession
  lookups: LookupData
  onChanged?: () => void | Promise<void>
}) {
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null)
  const [showAssemblyHistory, setShowAssemblyHistory] = useState(false)
  const [reservationMonth, setReservationMonth] = useState(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  })
  const formFields = withLookupOptions(fieldsForSession(module, canManage), lookups)
  const editFields = module.editFields ? withLookupOptions(module.editFields, lookups) : formFields
  const canCreate = Boolean(formFields?.length && module.createEndpoint && (canManage || ['reservations', 'visitors'].includes(module.id)))
  const isReadOnly = !canCreate
  const visibleRows = module.id === 'reservations' ? filterRowsByMonth(rows, reservationMonth, 'date') : rows

  async function load() {
    if (!module.listEndpoint) return
    setLoading(true)
    setMessage('')
    try {
      const listEndpoint = module.id === 'assembly' && showAssemblyHistory
        ? `${module.listEndpoint}?history=true`
        : module.listEndpoint
      const data = await requestJson<Record<string, unknown>[]>(listEndpoint)
      setRows(Array.isArray(data) ? data : [])
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [module.id, showAssemblyHistory])

  async function create(payload: Record<string, unknown>) {
    if (!module.createEndpoint) return
    const body = { ...payload }
    if (module.id === 'reservations' && !canManage) {
      body.userId = session.userId
      body.condominiumId = session.condominiumId
      body.status = 'pendente'
    }
    if (module.id === 'visitors' && !canManage) {
      body.residentId = session.userId
      body.condominiumId = session.condominiumId
    }
    if (module.id === 'assembly') {
      body.createdBy = session.userId
      body.startDate = dateInputToIso(body.startDate)
      body.endDate = dateInputToIso(body.endDate, true)
      body.scheduledDate = body.startDate
    }
    const created = await requestJson<Record<string, unknown>>(module.createEndpoint, {
      method: 'POST',
      headers: module.createHeaders?.(body),
      body: JSON.stringify(body),
    })
    await load()
    await onChanged?.()
    if (module.id === 'users' && created?.registrationNumber) {
      setMessage(`Usuário criado com sucesso. Login com o e-mail: ${created.registrationNumber}`)
      return
    }
    setMessage(createSuccessMessages[module.id] ?? 'Registro criado com sucesso.')
  }

  async function update(payload: Record<string, unknown>) {
    const id = editingRow ? normalizeId(editingRow) : ''
    const updateEndpoint = module.listEndpoint || module.createEndpoint
    if (!id || !updateEndpoint) return
    const body = { ...payload }
    if (module.id === 'assembly') {
      body.createdBy = String(editingRow?.createdBy ?? session.userId)
      body.startDate = dateInputToIso(body.startDate)
      body.endDate = dateInputToIso(body.endDate, true)
      body.scheduledDate = body.startDate
    }
    await requestJson(`${updateEndpoint}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
    setEditingRow(null)
    await load()
    await onChanged?.()
    setMessage(updateSuccessMessages[module.id] ?? 'Registro atualizado com sucesso.')
  }

  async function remove(row: Record<string, unknown>) {
    const id = normalizeId(row)
    const deleteEndpoint = module.listEndpoint || module.createEndpoint
    if (!id || !deleteEndpoint) {
      setMessage('Não foi possível identificar o registro para remoção.')
      return
    }
    if (!window.confirm(deleteConfirmationMessage(module.id))) return
    try {
      await requestJson(`${deleteEndpoint}/${id}`, { method: 'DELETE' })
      await load()
      await onChanged?.()
      setMessage(deleteSuccessMessages[module.id] ?? 'Registro removido com sucesso.')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : String(error))
    }
  }

  return (
    <div className={isReadOnly ? 'panel-grid read-only' : 'panel-grid'}>
      {canCreate && formFields && (
        <JsonForm
          title={module.id === 'reservations' && !canManage ? 'Nova reserva' : 'Novo registro'}
          fields={formFields}
          onSubmit={create}
          successMessage=""
        />
      )}
      {canManage && editFields && editingRow && (
        <JsonForm
          key={normalizeId(editingRow)}
          title="Editar registro"
          fields={editFields}
          initialData={editingRow}
          submitLabel="Atualizar"
          onSubmit={update}
          successMessage=""
          onCancel={() => setEditingRow(null)}
        />
      )}
      <section className="panel">
        <PanelHeader title="Registros" loading={loading} onRefresh={module.listEndpoint ? load : undefined} />
        {module.id === 'assembly' && (
          <div className="filter-row">
            <button type="button" className={!showAssemblyHistory ? '' : 'secondary-button'} onClick={() => setShowAssemblyHistory(false)}>
              Assembleias atuais
            </button>
            <button type="button" className={showAssemblyHistory ? '' : 'secondary-button'} onClick={() => setShowAssemblyHistory(true)}>
              Histórico
            </button>
          </div>
        )}
        {message && (
          <div className={isSuccessMessage(message) ? 'notice' : 'notice error'}>
            {message}
          </div>
        )}
        {module.id === 'reservations' && (
          <ReservationCalendar
            rows={rows}
            lookups={lookups}
            currentMonth={reservationMonth}
            onMonthChange={setReservationMonth}
          />
        )}
        <DataTable
          rows={visibleRows}
          columns={module.columns}
          lookups={lookups}
          onEdit={canManage && editFields ? row => setEditingRow(row) : undefined}
          onDelete={canManage && (module.listEndpoint || module.createEndpoint) && !module.noDelete ? remove : undefined}
          deleteLabel={module.id === 'users' ? 'Desativar' : undefined}
        />
        {(canManage || isFrontDesk(session)) && module.id === 'visitors' && <VisitorActions onDone={load} lookups={lookups} />}
        {canManage && module.id === 'deliveries' && <DeliveryActions onDone={load} lookups={lookups} />}
      </section>
    </div>
  )
}

function ReservationCalendar({
  rows,
  lookups,
  currentMonth,
  onMonthChange,
}: {
  rows: Record<string, unknown>[]
  lookups: LookupData
  currentMonth: Date
  onMonthChange: (month: Date) => void
}) {
  const monthLabel = currentMonth.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  const firstDay = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1)
  const startOffset = firstDay.getDay()
  const daysInMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 0).getDate()
  const cells = Array.from({ length: startOffset + daysInMonth }, (_, index) =>
    index < startOffset ? null : index - startOffset + 1
  )

  function moveMonth(offset: number) {
    onMonthChange(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + offset, 1))
  }

  function reservationsFor(day: number) {
    const dateKey = `${currentMonth.getFullYear()}-${String(currentMonth.getMonth() + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    return rows.filter(row => String(row.date ?? '').slice(0, 10) === dateKey)
  }

  return (
    <div className="calendar-panel">
      <div className="calendar-header">
        <button type="button" className="secondary-button" onClick={() => moveMonth(-1)}>Anterior</button>
        <strong>{monthLabel}</strong>
        <button type="button" className="secondary-button" onClick={() => moveMonth(1)}>Próximo</button>
      </div>
      <div className="calendar-weekdays">
        {['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'].map(day => <span key={day}>{day}</span>)}
      </div>
      <div className="calendar-grid">
        {cells.map((day, index) => (
          <div className={day ? 'calendar-day' : 'calendar-day muted-day'} key={`${day ?? 'empty'}-${index}`}>
            {day && (
              <>
                <span className="calendar-date">{day}</span>
                <div className="calendar-events">
                  {reservationsFor(day).map((reservation, eventIndex) => {
                    const area = formatCell('commonAreaId', reservation.commonAreaId, lookups)
                    const status = formatValue(reservation.status)
                    return (
                      <div className="calendar-event" key={`${normalizeId(reservation)}-${eventIndex}`}>
                        <strong>{area}</strong>
                        <span>{reservation.startTime as string} - {reservation.endTime as string}</span>
                        <em>{status}</em>
                      </div>
                    )
                  })}
                </div>
              </>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}

function UnitsPage({ module, canManage, lookups }: { module: ModuleDef; canManage: boolean; lookups: LookupData }) {
  const [condominiumId, setCondominiumId] = useState('')
  const endpointPath = condominiumId ? `/api/Units/condominium/${encodeURIComponent(condominiumId)}` : ''
  return (
    <FilteredPage
      module={{ ...module, listEndpoint: endpointPath || undefined }}
      canManage={canManage}
      lookups={lookups}
      filters={
        <div className="filter-row">
          <select value={condominiumId} onChange={event => setCondominiumId(event.target.value)}>
            <option value="">Selecione o condomínio</option>
            {lookups.condominiums.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </div>
      }
    />
  )
}

const petFilters = [
  { label: 'Por condomínio', lookupKey: 'condominiums' as LookupKey, build: (value: string) => `/api/Pets/condominium/${encodeURIComponent(value)}` },
  { label: 'Por unidade', lookupKey: 'units' as LookupKey, build: (value: string) => `/api/Pets/unit/${encodeURIComponent(value)}` },
  { label: 'Por tutor', lookupKey: 'users' as LookupKey, build: (value: string) => `/api/Pets/owner/${encodeURIComponent(value)}` },
]

const occurrenceFilters = [
  { label: 'Por condomínio', lookupKey: 'condominiums' as LookupKey, build: (value: string) => `/api/Occurrences/condominium/${encodeURIComponent(value)}` },
  { label: 'Por usuário', lookupKey: 'users' as LookupKey, build: (value: string) => `/api/Occurrences/user/${encodeURIComponent(value)}` },
]

// A API só lista cobranças por morador (GET /api/Bills/user/{userId}).
const billFilters = [
  { label: 'Por morador', lookupKey: 'users' as LookupKey, build: (value: string) => `/api/Bills/user/${encodeURIComponent(value)}` },
]

function FilteredPage({
  module,
  filters,
  canManage,
  lookups,
}: {
  module: ModuleDef
  filters: ReactNode | { label: string; lookupKey: LookupKey; build: (value: string) => string }[]
  canManage: boolean
  lookups: LookupData
}) {
  const [selected, setSelected] = useState(0)
  const [filterValue, setFilterValue] = useState('')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [message, setMessage] = useState('')
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null)
  const formFields = withLookupOptions(module.fields, lookups)
  const editFields = withLookupOptions(module.editFields ?? module.fields, lookups)

  const selectedFilter = Array.isArray(filters) ? filters[selected] : undefined
  const selectedOptions = selectedFilter ? lookups[selectedFilter.lookupKey] : []
  const filterNode = Array.isArray(filters) ? (
    <div className="filter-row">
      <select value={selected} onChange={event => setSelected(Number(event.target.value))}>
        {filters.map((filter, index) => <option key={filter.label} value={index}>{filter.label}</option>)}
      </select>
      <select value={filterValue} onChange={event => setFilterValue(event.target.value)}>
        <option value="">Selecione</option>
        {selectedOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
      <button type="button" onClick={loadFiltered}>Consultar</button>
    </div>
  ) : filters

  async function loadFiltered() {
    const endpointPath = module.listEndpoint || (Array.isArray(filters) && filterValue ? filters[selected].build(filterValue) : '')
    if (!endpointPath) return
    try {
      const data = await requestJson<Record<string, unknown>[]>(endpointPath)
      setRows(Array.isArray(data) ? data : [])
      setMessage('')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao consultar.')
    }
  }

  useEffect(() => {
    if (module.listEndpoint) loadFiltered()
  }, [module.listEndpoint])

  async function create(payload: Record<string, unknown>) {
    if (!module.createEndpoint) return
    await requestJson(module.createEndpoint, {
      method: 'POST',
      headers: module.createHeaders?.(payload),
      body: JSON.stringify(payload),
    })
    await loadFiltered()
    setMessage(createSuccessMessages[module.id] ?? 'Registro criado com sucesso.')
  }

  async function update(payload: Record<string, unknown>) {
    const id = editingRow ? normalizeId(editingRow) : ''
    if (!id || !module.createEndpoint) return
    await requestJson(`${module.createEndpoint}/${id}`, {
      method: 'PUT',
      headers: module.createHeaders?.(payload),
      body: JSON.stringify(payload),
    })
    setEditingRow(null)
    await loadFiltered()
    setMessage(updateSuccessMessages[module.id] ?? 'Registro atualizado com sucesso.')
  }

  async function remove(row: Record<string, unknown>) {
    const id = normalizeId(row)
    const deleteEndpoint = module.createEndpoint || module.listEndpoint
    if (!id || !deleteEndpoint) {
      setMessage('Não foi possível identificar o registro para remoção.')
      return
    }
    if (!window.confirm(deleteConfirmationMessage(module.id))) return
    try {
      await requestJson(`${deleteEndpoint}/${id}`, { method: 'DELETE' })
      await loadFiltered()
      setMessage(deleteSuccessMessages[module.id] ?? 'Registro removido com sucesso.')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao remover.')
    }
  }

  return (
    <div className="panel-grid">
      {canManage && formFields && (
        <JsonForm
          title="Novo registro"
          fields={formFields}
          onSubmit={create}
          successMessage=""
        />
      )}
      {canManage && editFields && editingRow && (
        <JsonForm
          key={normalizeId(editingRow)}
          title="Editar registro"
          fields={editFields}
          initialData={editingRow}
          submitLabel="Atualizar"
          onSubmit={update}
          successMessage=""
          onCancel={() => setEditingRow(null)}
        />
      )}
      <section className="panel">
        <h2>Consulta</h2>
        {filterNode}
        {message && <div className={isSuccessMessage(message) ? 'notice' : 'notice error'}>{message}</div>}
        <DataTable
          rows={rows}
          columns={module.columns}
          lookups={lookups}
          onEdit={canManage && editFields ? row => setEditingRow(row) : undefined}
          onDelete={canManage && module.createEndpoint && !module.noDelete ? remove : undefined}
        />
      </section>
    </div>
  )
}

function VotesPage({ module, canManage, session, lookups }: { module: ModuleDef; canManage: boolean; session: AppSession; lookups: LookupData }) {
  const [assemblyId, setAssemblyId] = useState('')
  const [topicIndex, setTopicIndex] = useState('')
  const [vote, setVote] = useState('')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [assemblyDetails, setAssemblyDetails] = useState<Record<string, unknown> | null>(null)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const selectedAssembly = lookups.assemblies.find(option => option.value === assemblyId)
  const topics = Array.isArray(assemblyDetails?.topics) ? assemblyDetails.topics as Record<string, unknown>[] : []
  const selectedTopic = topicIndex === '' ? undefined : topics[Number(topicIndex)]
  const voteOptions = Array.isArray(selectedTopic?.voteOptions)
    ? selectedTopic.voteOptions.map(option => String(option))
    : ['sim', 'não']
  const topicRows = topicIndex === '' ? [] : rows.filter(row => Number(row.topicIndex) === Number(topicIndex))
  const historyRows = rows.filter(row => !row.summaryOnly)
  const voteSummary = topicRows[0] ? {
    yesVotes: Number(topicRows[0].yesVotes ?? 0),
    noVotes: Number(topicRows[0].noVotes ?? 0),
    winner: String(topicRows[0].winner ?? 'Empate'),
    voteFinished: Boolean(topicRows[0].voteFinished ?? false),
  } : null

  async function loadVotes(nextAssemblyId = assemblyId) {
    if (!nextAssemblyId) {
      setAssemblyDetails(null)
      setRows([])
      return
    }
    setMessage('')
    try {
      const [details, data] = await Promise.all([
        requestJson<Record<string, unknown>>(`/api/assembly/${encodeURIComponent(nextAssemblyId)}`),
        requestJson<Record<string, unknown>[]>(`/api/vote/assembly/${encodeURIComponent(nextAssemblyId)}`),
      ])
      setAssemblyDetails(details)
      const safeRows = Array.isArray(data) ? data : []
      setRows(safeRows)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar a votação.')
    }
  }

  async function createVote(event: FormEvent) {
    event.preventDefault()
    if (!assemblyId) return setMessage('Selecione a assembleia.')
    if (topicIndex === '') return setMessage('Selecione a pauta.')
    if (!session.userId) return setMessage('Usuário não autenticado.')
    if (!vote) return setMessage('Escolha uma opção de voto.')
    if (rows.some(row => !row.summaryOnly && String(row.userId ?? '') === session.userId && Number(row.topicIndex) === Number(topicIndex))) {
      return setMessage('O usuário já votou nesta pauta.')
    }

    setBusy(true)
    setMessage('')
    try {
      await requestJson('/api/vote', {
        method: 'POST',
        body: JSON.stringify({ assemblyId, topicIndex: Number(topicIndex), vote }),
      })
      setMessage('Voto registrado.')
      setVote('')
      await loadVotes()
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao registrar voto.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="panel-grid">
      <section className="panel">
        <h2>Novo voto</h2>
        <form className="form-grid" onSubmit={createVote}>
          <label>
            <span>Assembleia *</span>
            <select value={assemblyId} onChange={event => {
              const nextAssemblyId = event.target.value
              setAssemblyId(nextAssemblyId)
              setTopicIndex('')
              setVote('')
              loadVotes(nextAssemblyId)
            }} required>
              <option value="">Selecione</option>
              {lookups.assemblies.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
          </label>
          <label>
            <span>Pauta *</span>
            <select value={topicIndex} onChange={event => { setTopicIndex(event.target.value); setVote('') }} required disabled={!topics.length}>
              <option value="">Selecione</option>
              {topics.map((topic, index) => <option key={index} value={index}>{String(topic.title ?? `Pauta ${index + 1}`)}</option>)}
            </select>
          </label>
          <label>
            <span>Voto *</span>
            <select value={vote} onChange={event => setVote(event.target.value)} required disabled={topicIndex === ''}>
              <option value="">Selecione</option>
              {voteOptions.map(option => <option key={option} value={option}>{formatOptionLabel(option)}</option>)}
            </select>
          </label>
          <div className="form-actions span-two">
            <button type="submit" disabled={busy || topicIndex === ''}>{busy ? 'Registrando...' : 'Registrar voto'}</button>
          </div>
        </form>
        {message && <div className={message === 'Voto registrado.' ? 'notice' : 'notice error'}>{message}</div>}
      </section>
      <section className="panel">
        <PanelHeader title="Assembleias" onRefresh={() => loadVotes()} />
        {selectedAssembly && (
          <div className="notice" style={{ marginTop: 8, display: 'grid', gap: 6 }}>
            <div><strong>Assembleia:</strong> {selectedAssembly.label}</div>
            {assemblyDetails?.description ? <div>{String(assemblyDetails.description)}</div> : null}
            <div><strong>Status da votação:</strong> {String(assemblyDetails?.voteStatus ?? 'Em andamento')}</div>
            <div><strong>Período:</strong> {formatDateTime(String(assemblyDetails?.startDate ?? assemblyDetails?.startTime))} até {formatDateTime(String(assemblyDetails?.endDate ?? assemblyDetails?.endTime))}</div>
            {selectedTopic && <div><strong>Pauta:</strong> {String(selectedTopic.title ?? '')}{selectedTopic.desc ? ` - ${String(selectedTopic.desc)}` : ''}</div>}
            {voteSummary?.voteFinished && (
              <div style={{ borderTop: '1px solid #e5e7eb', paddingTop: 6 }}>
                <strong>Resultado final:</strong> Sim: {voteSummary.yesVotes} votos · Não: {voteSummary.noVotes} votos
                <br />
                <strong>Vencedor:</strong> {voteSummary.winner}
              </div>
            )}
          </div>
        )}
        {assemblyDetails && (
          <div className="topic-results-grid">
            {topics.map((topic, index) => {
              const summary = rows.find(row => Number(row.topicIndex) === index)
              const counts = summary?.optionCounts && typeof summary.optionCounts === 'object'
                ? summary.optionCounts as Record<string, number>
                : {}
              return (
                <article className="topic-result-card" key={index}>
                  <strong>{String(topic.title ?? `Pauta ${index + 1}`)}</strong>
                  {Object.entries(counts).length
                    ? Object.entries(counts).map(([option, count]) => <span key={option}>{formatOptionLabel(option)}: {count}</span>)
                    : <span>Nenhum voto registrado.</span>}
                  <span>Total da pauta: {Number(summary?.totalVotes ?? 0)}</span>
                </article>
              )
            })}
          </div>
        )}
        <DataTable
          rows={topicIndex === '' ? historyRows : topicRows.filter(row => !row.summaryOnly)}
          columns={canManage ? module.columns : module.columns?.filter(column => column !== 'userId')}
          lookups={lookups}
        />
      </section>
    </div>
  )
}


function billOptionLabel(bill: Record<string, unknown>) {
  const dueDate = typeof bill.dueDate === 'string' ? formatDateOnly(bill.dueDate) : '-'
  return `${String(bill.description ?? 'Cobrança')} - ${formatCurrency(bill.amount)} - venc. ${dueDate} - ${formatValue(bill.status)}`
}

// A API só lista pagamentos por cobrança (GET /api/Payments/bill/{billId}) e cobranças por morador:
// o administrador escolhe o morador e depois a cobrança; o morador já vê as próprias cobranças.
function PaymentsPage({ module, canManage, session, lookups }: { module: ModuleDef; canManage: boolean; session: AppSession; lookups: LookupData }) {
  const [userId, setUserId] = useState(canManage ? '' : session.userId)
  const [bills, setBills] = useState<Record<string, unknown>[]>([])
  const [billId, setBillId] = useState('')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [message, setMessage] = useState('')
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null)
  const fields = module.fields ?? []
  const selectedBill = bills.find(bill => normalizeId(bill) === billId)

  async function fetchBills(targetUserId: string) {
    const data = await requestJson<Record<string, unknown>[]>(`/api/Bills/user/${encodeURIComponent(targetUserId)}`)
    setBills(Array.isArray(data) ? data : [])
  }

  async function loadPayments(targetBillId: string) {
    setRows([])
    if (!targetBillId) return
    try {
      const data = await requestJson<Record<string, unknown>[]>(`/api/Payments/bill/${encodeURIComponent(targetBillId)}`)
      setRows(Array.isArray(data) ? data : [])
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar os pagamentos.')
    }
  }

  async function selectUser(nextUserId: string) {
    setUserId(nextUserId)
    setBills([])
    setBillId('')
    setRows([])
    setEditingRow(null)
    setMessage('')
    if (!nextUserId) return
    try {
      await fetchBills(nextUserId)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar as cobranças.')
    }
  }

  async function selectBill(nextBillId: string) {
    setBillId(nextBillId)
    setEditingRow(null)
    setMessage('')
    await loadPayments(nextBillId)
  }

  useEffect(() => {
    if (!canManage && session.userId) selectUser(session.userId)
  }, [canManage, session.userId])

  async function create(payload: Record<string, unknown>) {
    if (!module.createEndpoint || !billId) return
    await requestJson(module.createEndpoint, {
      method: 'POST',
      body: JSON.stringify({ billId, ...payload }),
    })
    // A API marca a cobrança como paga ao registrar o pagamento: recarrega as duas listas.
    await Promise.all([fetchBills(userId), loadPayments(billId)])
    setMessage(createSuccessMessages.payments ?? 'Registro criado com sucesso.')
  }

  async function update(payload: Record<string, unknown>) {
    const id = editingRow ? normalizeId(editingRow) : ''
    if (!id || !module.createEndpoint) return
    await requestJson(`${module.createEndpoint}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
    setEditingRow(null)
    await loadPayments(billId)
    setMessage(updateSuccessMessages.payments ?? 'Registro atualizado com sucesso.')
  }

  return (
    <div className={canManage ? 'panel-grid' : 'panel-grid read-only'}>
      {canManage && billId && (
        <JsonForm title="Novo pagamento" fields={fields} onSubmit={create} submitLabel="Registrar pagamento" successMessage="" />
      )}
      {canManage && editingRow && (
        <JsonForm
          key={normalizeId(editingRow)}
          title="Editar pagamento"
          fields={fields}
          initialData={editingRow}
          submitLabel="Atualizar"
          onSubmit={update}
          successMessage=""
          onCancel={() => setEditingRow(null)}
        />
      )}
      <section className="panel">
        <PanelHeader title="Pagamentos" onRefresh={billId ? () => loadPayments(billId) : undefined} />
        <div className="form-grid">
          {canManage && (
            <label>
              <span>Morador</span>
              <select value={userId} onChange={event => selectUser(event.target.value)}>
                <option value="">Selecione</option>
                {lookups.users.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
          )}
          <label>
            <span>Cobrança</span>
            <select value={billId} onChange={event => selectBill(event.target.value)} disabled={!bills.length}>
              <option value="">{userId && !bills.length ? 'Nenhuma cobrança' : 'Selecione'}</option>
              {bills.map(bill => <option key={normalizeId(bill)} value={normalizeId(bill)}>{billOptionLabel(bill)}</option>)}
            </select>
          </label>
        </div>
        {selectedBill && (
          <div className="notice" style={{ marginTop: 8 }}>
            <strong>Cobrança:</strong> {billOptionLabel(selectedBill)}
          </div>
        )}
        {message && <div className={isSuccessMessage(message) ? 'notice' : 'notice error'}>{message}</div>}
        <DataTable
          rows={rows}
          columns={module.columns}
          lookups={lookups}
          onEdit={canManage ? row => setEditingRow(row) : undefined}
        />
      </section>
    </div>
  )
}

function VisitorActions({ onDone, lookups }: { onDone: () => void; lookups: LookupData }) {
  const [visitorId, setVisitorId] = useState('')
  async function post(action: 'check-in' | 'check-out') {
    await requestJson(`/api/Visitor/${action}/${visitorId}`, { method: 'POST' })
    onDone()
  }
  return (
    <div className="inline-actions">
      <select value={visitorId} onChange={event => setVisitorId(event.target.value)}>
        <option value="">Selecione o visitante</option>
        {lookups.visitors.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
      <button type="button" onClick={() => post('check-in')}>Entrada</button>
      <button type="button" onClick={() => post('check-out')}>Saída</button>
    </div>
  )
}

function DeliveryActions({ onDone, lookups }: { onDone: () => void; lookups: LookupData }) {
  const [id, setId] = useState('')
  const [pickedUpBy, setPickedUpBy] = useState('')

  async function pickup() {
    if (!id) return
    await requestJson(`/api/Delivery/${id}/pickup?pickedUpBy=${encodeURIComponent(pickedUpBy)}`, { method: 'PUT' })
    onDone()
  }

  return (
    <div className="inline-actions stacked">
      <div>
        <select value={id} onChange={event => setId(event.target.value)}>
          <option value="">Selecione a encomenda</option>
          {lookups.deliveries.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
        <input value={pickedUpBy} onChange={event => setPickedUpBy(event.target.value)} placeholder="Retirado por" />
        <button type="button" onClick={pickup} disabled={!id}>Retirada</button>
      </div>
    </div>
  )
}

function JsonForm({
  title,
  fields,
  onSubmit,
  onCancel,
  submitting = false,
  initialData,
  submitLabel = 'Salvar',
  successMessage = 'Salvo.',
}: {
  title: string
  fields: FieldDef[]
  onSubmit: (payload: Record<string, unknown>) => Promise<unknown> | unknown
  onCancel?: () => void
  submitting?: boolean
  initialData?: Record<string, unknown>
  submitLabel?: string
  successMessage?: string
}) {
  const initial = useMemo(
    () => Object.fromEntries(fields.map(field => [field.name, formInitialValue(field, initialData)])),
    [fields, initialData]
  )
  const [data, setData] = useState<Record<string, unknown>>(initial)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => setData(initial), [initial])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setMessage('')
    try {
      const today = todayInputValue()
      const invalidDate = fields.find(field =>
        field.disallowPast &&
        typeof data[field.name] === 'string' &&
        Boolean(dateBRToApi(String(data[field.name]))) &&
        String(dateBRToApi(String(data[field.name]))) < today
      )
      if (invalidDate) {
        throw new Error(`${invalidDate.label} não pode ser anterior a hoje.`)
      }
      await onSubmit(buildPayload(fields, data))
      setMessage(successMessage)
      setData(initial)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao salvar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel">
      <h2>{title}</h2>
      <form className="form-grid" onSubmit={submit}>
        {fields.map(field => (
          <label key={field.name} className={field.type === 'textarea' ? 'span-two' : ''}>
            <span>{field.label}{field.required ? ' *' : ''}</span>
            {renderInput(field, data[field.name], value => setData(current => ({ ...current, [field.name]: value })))}
          </label>
        ))}
        <div className="form-actions span-two">
          {onCancel && <button type="button" className="secondary-button" onClick={onCancel}>Cancelar</button>}
          <button type="submit" disabled={busy || submitting}>{busy || submitting ? 'Salvando...' : submitLabel}</button>
        </div>
      </form>
      {message && <div className={message === successMessage ? 'notice' : 'notice error'}>{message}</div>}
    </section>
  )
}

function renderInput(field: FieldDef, value: unknown, onChange: (value: unknown) => void) {
  if (field.type === 'textarea') {
    return <textarea value={String(value ?? '')} onChange={event => onChange(event.target.value)} placeholder={field.placeholder} required={field.required} />
  }
  if (field.type === 'select' || field.selectOptions?.length) {
    const options = field.selectOptions ?? (field.options ?? []).map(option => ({ value: option, label: formatOptionLabel(option) }))
    return (
      <select value={String(value ?? '')} onChange={event => onChange(event.target.value)} required={field.required}>
        <option value="">Selecione</option>
        {options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    )
  }
  if (field.type === 'checkbox') {
    return <input type="checkbox" checked={Boolean(value)} onChange={event => onChange(event.target.checked)} />
  }
  if (field.name === 'cnpj') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={18}
        value={String(value ?? '')}
        onChange={event => onChange(maskCnpj(event.target.value))}
        placeholder="00.000.000/0000-00"
        required={field.required}
      />
    )
  }
  if (field.name === 'cpf') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={14}
        value={String(value ?? '')}
        onChange={event => onChange(maskCpf(event.target.value))}
        placeholder="000.000.000-00"
        required={field.required}
      />
    )
  }
  if (field.name === 'zipCode') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={9}
        value={String(value ?? '')}
        onChange={event => onChange(maskCep(event.target.value))}
        placeholder="00000-000"
        required={field.required}
      />
    )
  }
  if (field.name === 'phone') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={12}
        value={String(value ?? '')}
        onChange={event => onChange(maskPhoneBR(event.target.value))}
        placeholder="31 9999-2702"
        required={field.required}
      />
    )
  }
  if (field.type === 'time') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={5}
        value={String(value ?? '')}
        onChange={event => onChange(maskTime(event.target.value))}
        placeholder="14:00"
        required={field.required}
      />
    )
  }
  if (field.name === 'birthDate' || field.type === 'date') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={10}
        value={String(value ?? '')}
        onChange={event => onChange(maskDateBR(event.target.value))}
        placeholder="DD/MM/AAAA"
        required={field.required}
      />
    )
  }
  return (
    <input
      type={field.type ?? 'text'}
      step={field.step}
      value={String(value ?? '')}
      onChange={event => onChange(event.target.value)}
      placeholder={field.placeholder}
      required={field.required}
    />
  )
}

function PanelHeader({ title, loading, onRefresh }: { title: string; loading?: boolean; onRefresh?: () => void }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      {onRefresh && <button type="button" onClick={onRefresh}>{loading ? 'Carregando...' : 'Atualizar'}</button>}
    </div>
  )
}

function DataTable({
  rows,
  columns,
  lookups,
  onEdit,
  onDelete,
  deleteLabel = 'Remover',
}: {
  rows: Record<string, unknown>[]
  columns?: string[]
  lookups?: LookupData
  onEdit?: (row: Record<string, unknown>) => void | Promise<void>
  onDelete?: (row: Record<string, unknown>) => void | Promise<void>
  deleteLabel?: string
}) {
  const visibleColumns = columns?.length
    ? columns
    : Array.from(new Set(rows.flatMap(row => Object.keys(row)))).slice(0, 6)

  if (!rows.length) return <div className="empty-state">Nenhum registro carregado.</div>

  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            {visibleColumns.map(column => <th key={column}>{labels[column] ?? column}</th>)}
            {(onEdit || onDelete) && <th>Ações</th>}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={normalizeId(row) || index}>
              {visibleColumns.map(column => <td key={column}>{formatCell(column, row[column], lookups)}</td>)}
              {(onEdit || onDelete) && (
                <td>
                  <div className="row-actions">
                    {onEdit && <button type="button" onClick={() => onEdit(row)}>Editar</button>}
                    {onDelete && <button className="danger" type="button" onClick={() => onDelete(row)}>{deleteLabel}</button>}
                  </div>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
