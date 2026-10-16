// Formatação do financeiro (cobranças e pagamentos) no app.

export type Tone = 'success' | 'warning' | 'danger' | 'neutral';

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export function formatCurrencyBR(value: number) {
  return currency.format(Number(value) || 0);
}

// A API manda a data como ISO (meia-noite UTC): usa só a parte da data, sem converter fuso.
export function formatDateBR(value?: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value ?? '');
  return match ? `${match[3]}/${match[2]}/${match[1]}` : '-';
}

const STATUS: Record<string, { label: string; tone: Tone }> = {
  pending: { label: 'Em aberto', tone: 'warning' },
  overdue: { label: 'Vencida', tone: 'danger' },
  paid: { label: 'Paga', tone: 'success' },
  canceled: { label: 'Cancelada', tone: 'neutral' },
};

export function billStatus(status: string) {
  return STATUS[status] ?? { label: status, tone: 'neutral' as Tone };
}

export function openTotal(bills: { amount: number; status: string }[]) {
  return bills
    .filter(bill => bill.status === 'pending' || bill.status === 'overdue')
    .reduce((sum, bill) => sum + Number(bill.amount || 0), 0);
}
