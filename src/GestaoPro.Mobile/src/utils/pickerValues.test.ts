import { describe, expect, it } from 'vitest';
import { dateFromDisplay, displayDate, displayTime, timeFromDisplay } from './pickerValues';

describe('valores do seletor de data e hora', () => {
  it('formata data e hora como o formulário espera', () => {
    const value = new Date(2026, 9, 4, 7, 5);
    expect(displayDate(value)).toBe('04/10/2026');
    expect(displayTime(value)).toBe('07:05');
  });

  it('volta do texto para Date (e usa hoje quando vazio ou inválido)', () => {
    expect(dateFromDisplay('15/06/2026')?.getMonth()).toBe(5);
    expect(dateFromDisplay('99/99/2026')).toBeNull();
    expect(timeFromDisplay('14:30')?.getHours()).toBe(14);
    expect(timeFromDisplay('25:00')).toBeNull();
  });
});
