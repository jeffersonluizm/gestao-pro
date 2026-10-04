// Conversão entre o texto dos formulários (DD/MM/AAAA e HH:MM) e o Date do seletor nativo.
const pad = (n: number) => String(n).padStart(2, '0');

export function displayDate(value: Date) {
  return `${pad(value.getDate())}/${pad(value.getMonth() + 1)}/${value.getFullYear()}`;
}

export function displayTime(value: Date) {
  return `${pad(value.getHours())}:${pad(value.getMinutes())}`;
}

export function dateFromDisplay(text: string): Date | null {
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(text);
  if (!match) return null;
  const [, d, m, y] = match.map(Number);
  const date = new Date(y, m - 1, d);
  return date.getDate() === d && date.getMonth() === m - 1 ? date : null;
}

export function timeFromDisplay(text: string): Date | null {
  const match = /^(\d{2}):(\d{2})$/.exec(text);
  if (!match) return null;
  const [, h, min] = match.map(Number);
  if (h > 23 || min > 59) return null;
  const date = new Date();
  date.setHours(h, min, 0, 0);
  return date;
}
