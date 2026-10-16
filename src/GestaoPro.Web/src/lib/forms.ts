import { AppSession, FieldDef, FieldType, LookupData, LookupKey, ModuleDef } from '../domain/types'
import { normalizeId } from './format'
import { formatOptionLabel } from './labels'
import { apiDateToBR, dateBRToApi, formatBirthDate, isValidCpfFormat, isValidTime, maskCep, maskCnpj, maskCpf, maskPhoneBR, maskTime, onlyDigits, parseBirthDate } from './masks'

export function fieldsForSession(module: ModuleDef, canManage: boolean) {
  if (module.id === 'reservations' && !canManage) {
    return module.fields?.filter(field => !['userId', 'condominiumId', 'status'].includes(field.name))
  }

  if (module.id === 'visitors' && !canManage) {
    return module.fields?.filter(field => !['residentId', 'condominiumId', 'status'].includes(field.name))
  }

  return module.fields
}

export function lookupKeyForField(fieldName: string): LookupKey | undefined {
  if (fieldName === 'condominiumId') return 'condominiums'
  if (fieldName === 'commonAreaId') return 'commonAreas'
  if (['userId', 'residentId', 'ownerId', 'recipientId', 'reservedByUserId'].includes(fieldName)) return 'users'
  if (fieldName === 'unitId') return 'units'
  if (fieldName === 'assemblyId') return 'assemblies'
  return undefined
}

export function withLookupOptions(fields: FieldDef[] | undefined, lookups: LookupData) {
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

export function parseTopicsInput(value: string) {
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

export function fieldInitialValue(field: FieldDef) {
  if (field.defaultValue !== undefined) return field.defaultValue
  if (field.type === 'checkbox') return false
  return ''
}

export function buildPayload(fields: FieldDef[], data: Record<string, unknown>) {
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

export function formInitialValue(field: FieldDef, initialData?: Record<string, unknown>) {
  // Senhas e códigos nunca vêm preenchidos com o que a API devolve (ela devolve mascarado).
  if (field.type === 'password') return fieldInitialValue(field)
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

export function normalizeOptionText(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .trim()
}

export function resolveSelectValue(field: FieldDef, value: string) {
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

// O condomínio vem sempre da sessão (o token só permite o próprio): o campo some dos formulários
// e o valor entra no corpo da requisição.
export function withoutSessionFields(fields: FieldDef[] | undefined) {
  return fields?.filter(field => field.name !== 'condominiumId')
}

export function withSessionCondominium(module: ModuleDef, payload: Record<string, unknown>, session: AppSession) {
  const usesCondominium = [...(module.fields ?? []), ...(module.editFields ?? [])].some(field => field.name === 'condominiumId')
  return usesCondominium ? { ...payload, condominiumId: session.condominiumId } : payload
}
