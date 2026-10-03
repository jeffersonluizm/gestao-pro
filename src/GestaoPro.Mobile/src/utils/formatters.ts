export function onlyDigits(value: string) {
  return value.replace(/\D/g, '');
}

export function maskCnpj(value: string) {
  const digits = onlyDigits(value).slice(0, 14);
  return digits
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2');
}

export function maskCpf(value: string) {
  const digits = onlyDigits(value).slice(0, 11);
  return digits
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1-$2');
}

export function isValidCpfFormat(value: string) {
  return /^\d{3}\.\d{3}\.\d{3}-\d{2}$/.test(value) && onlyDigits(value).length === 11;
}

export function maskCep(value: string) {
  const digits = onlyDigits(value).slice(0, 8);
  return digits.replace(/^(\d{5})(\d)/, '$1-$2');
}

export function maskDateBR(value: string) {
  const digits = onlyDigits(value).slice(0, 8);
  if (digits.length <= 2) return digits;
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`;
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
}

export function maskPhoneBR(value: string) {
  const digits = onlyDigits(value).slice(0, 10);
  if (digits.length <= 2) return digits;
  if (digits.length <= 6) return `${digits.slice(0, 2)} ${digits.slice(2)}`;
  return `${digits.slice(0, 2)} ${digits.slice(2, 6)}-${digits.slice(6)}`;
}

export function maskTime(value: string) {
  const digits = onlyDigits(value).slice(0, 4);
  if (digits.length <= 2) return digits;
  return `${digits.slice(0, 2)}:${digits.slice(2)}`;
}

export function isValidTime(value: string) {
  const match = /^(\d{2}):(\d{2})$/.exec(value);
  if (!match) return false;
  const [, hours, minutes] = match;
  return Number(hours) >= 0 && Number(hours) <= 23
    && Number(minutes) >= 0 && Number(minutes) <= 59;
}

export function isValidDateBR(value: string) {
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value);
  if (!match) return false;
  const [, day, month, year] = match;
  const date = new Date(Number(year), Number(month) - 1, Number(day));
  return date.getFullYear() === Number(year)
    && date.getMonth() + 1 === Number(month)
    && date.getDate() === Number(day);
}

export function dateBRToApi(value: string) {
  if (!isValidDateBR(value)) return null;
  const [day, month, year] = value.split('/');
  return `${year}-${month}-${day}`;
}

export function dateBRToIso(value: string, endOfDay = false) {
  const apiDate = dateBRToApi(value);
  if (!apiDate) return null;
  const [year, month, day] = apiDate.split('-').map(Number);
  return new Date(
    year,
    month - 1,
    day,
    endOfDay ? 23 : 0,
    endOfDay ? 59 : 0,
    endOfDay ? 59 : 0,
    endOfDay ? 999 : 0,
  ).toISOString();
}

export function apiDateToBR(value?: string) {
  if (!value) return '';
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (match) return `${match[3]}/${match[2]}/${match[1]}`;
  const date = new Date(value);
  return Number.isFinite(date.getTime()) ? date.toLocaleDateString('pt-BR') : '';
}
