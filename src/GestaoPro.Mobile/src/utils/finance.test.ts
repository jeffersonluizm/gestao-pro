import { describe, expect, it } from 'vitest';
import { billStatus, formatCurrencyBR, formatDateBR, openTotal } from './finance';

describe('financeiro', () => {
  it('formata valor em reais', () => {
    expect(formatCurrencyBR(620)).toMatch(/^R\$\s620,00$/);
  });

  it('formata data da API (ISO) em DD/MM/AAAA sem mudar o dia pelo fuso', () => {
    expect(formatDateBR('2026-10-14T00:00:00Z')).toBe('14/10/2026');
  });

  it('traduz o status da cobrança com tom de cor', () => {
    expect(billStatus('pending')).toEqual({ label: 'Em aberto', tone: 'warning' });
    expect(billStatus('overdue')).toEqual({ label: 'Vencida', tone: 'danger' });
    expect(billStatus('paid')).toEqual({ label: 'Paga', tone: 'success' });
  });

  it('soma só as cobranças em aberto e vencidas', () => {
    expect(openTotal([
      { amount: 620, status: 'pending' },
      { amount: 100, status: 'overdue' },
      { amount: 620, status: 'paid' },
    ])).toBe(720);
  });
});
