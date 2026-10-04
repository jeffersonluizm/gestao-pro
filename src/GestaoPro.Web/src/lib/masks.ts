export function todayInputValue() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

export function onlyDigits(value: string) {
  return value.replace(/\D/g, '')
}

export function maskCnpj(value: string) {
  return onlyDigits(value)
    .slice(0, 14)
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2')
}

export function maskCpf(value: string) {
  return onlyDigits(value)
    .slice(0, 11)
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1-$2')
}

export function isValidCpfFormat(value: string) {
  return /^\d{3}\.\d{3}\.\d{3}-\d{2}$/.test(value) && onlyDigits(value).length === 11
}

export function maskCep(value: string) {
  return onlyDigits(value).slice(0, 8).replace(/^(\d{5})(\d)/, '$1-$2')
}

export function maskDateBR(value: string) {
  const digits = onlyDigits(value).slice(0, 8)
  if (digits.length <= 2) return digits
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`
}

export function maskPhoneBR(value: string) {
  const digits = onlyDigits(value).slice(0, 10)
  if (digits.length <= 2) return digits
  if (digits.length <= 6) return `${digits.slice(0, 2)} ${digits.slice(2)}`
  return `${digits.slice(0, 2)} ${digits.slice(2, 6)}-${digits.slice(6)}`
}

export function maskTime(value: string) {
  const digits = onlyDigits(value).slice(0, 4)
  if (digits.length <= 2) return digits
  return `${digits.slice(0, 2)}:${digits.slice(2)}`
}

export function isValidTime(value: string) {
  const match = /^(\d{2}):(\d{2})$/.exec(value)
  if (!match) return false
  const [, hours, minutes] = match
  return Number(hours) >= 0 && Number(hours) <= 23
    && Number(minutes) >= 0 && Number(minutes) <= 59
}

export function dateBRToApi(value: string) {
  const match = value.trim().match(/^(\d{2})\/(\d{2})\/(\d{4})$/)
  if (!match) return null
  const [, day, month, year] = match
  const date = new Date(Number(year), Number(month) - 1, Number(day))
  if (date.getFullYear() !== Number(year) || date.getMonth() + 1 !== Number(month) || date.getDate() !== Number(day)) return null
  return `${year}-${month}-${day}`
}

export function apiDateToBR(value: string) {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value
}

export function formatBirthDate(value: string) {
  return apiDateToBR(value)
}

export function parseBirthDate(value: string) {
  return dateBRToApi(value)
}

export function dateInputToIso(value: unknown, endOfDay = false) {
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
