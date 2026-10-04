import { LookupData } from '../domain/types'
import { lookupKeyForField } from './forms'
import { formatOptionLabel, optionLabels, statusLabels } from './labels'

export function rowId(row: Record<string, unknown>) {
  return String(row.id ?? row._id ?? '')
}

export function optionFromRow(row: Record<string, unknown>, fallback: string) {
  const id = rowId(row)
  if (!id) return undefined
  const name = String(row.name ?? row.fullName ?? row.title ?? row.description ?? fallback)
  const registration = row.registrationNumber ? ` - matrícula ${row.registrationNumber}` : ''
  const unit = row.number ? ` - ${row.block ? `Bloco ${row.block} / ` : ''}${row.number}` : ''
  return { value: id, label: `${name}${registration}${unit}` }
}


export function formatCell(column: string, value: unknown, lookups?: LookupData) {
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

export function normalizeId(row: Record<string, unknown>) {
  const mongoId = typeof row._id === 'object' && row._id !== null
    ? (row._id as Record<string, unknown>).$oid
    : row._id
  return String(row.id ?? mongoId ?? row.userId ?? row.usuarioId ?? row.visitorId ?? '').trim()
}

export function formatValue(value: unknown) {
  if (value === null || value === undefined || value === '') return '-'
  if (typeof value === 'string' && (statusLabels[value] || optionLabels[value])) return formatOptionLabel(value)
  if (typeof value === 'boolean') return value ? 'Sim' : 'Não'
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) return new Date(value).toLocaleString('pt-BR')
  if (Array.isArray(value)) return `${value.length} item(ns)`
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

export function formatDateOnly(value: string) {
  if (!value) return '-'
  const datePart = value.slice(0, 10)
  const [year, month, day] = datePart.split('-')
  if (!year || !month || !day) return formatValue(value)
  return `${day}/${month}/${year}`
}

export function formatDateTime(value: string | undefined) {
  if (!value) return '-'

  const text = String(value).trim()
  const slashMatch = text.match(/^(\d{2})\/(\d{2})\/(\d{4})/)
  if (slashMatch) return `${slashMatch[1]}/${slashMatch[2]}/${slashMatch[3]}`

  const isoMatch = text.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (isoMatch) return `${isoMatch[3]}/${isoMatch[2]}/${isoMatch[1]}`

  const date = new Date(text)
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleDateString('pt-BR')
}
