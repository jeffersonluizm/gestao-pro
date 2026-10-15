import { beforeEach, describe, expect, it, vi } from 'vitest';
import { enterDemoWithRetry, forgetDemoRole, markSessionExpired, rememberDemoRole, takeAutoRetryRole } from './demoSession';

class FakeApiError extends Error {
  constructor(message: string, readonly status: number) { super(message); }
}

describe('enterDemoWithRetry', () => {
  it('entra de primeira quando a demonstração está pronta', async () => {
    const enter = vi.fn(async (role: string) => ({ role }));

    await expect(enterDemoWithRetry(enter, 'morador', async () => undefined)).resolves.toEqual({ role: 'morador' });
    expect(enter).toHaveBeenCalledTimes(1);
  });

  it('demonstração sendo preparada (503): espera e tenta mais uma vez', async () => {
    const enter = vi.fn()
      .mockRejectedValueOnce(new FakeApiError('Preparando.', 503))
      .mockResolvedValueOnce({ role: 'sindico' });
    const wait = vi.fn(async () => undefined);

    await expect(enterDemoWithRetry(enter, 'sindico', wait)).resolves.toEqual({ role: 'sindico' });
    expect(enter).toHaveBeenCalledTimes(2);
    expect(wait).toHaveBeenCalledTimes(1);
  });

  it('outro erro não é repetido', async () => {
    const enter = vi.fn().mockRejectedValue(new FakeApiError('Muitas tentativas.', 429));

    await expect(enterDemoWithRetry(enter, 'morador', async () => undefined)).rejects.toThrow('Muitas tentativas.');
    expect(enter).toHaveBeenCalledTimes(1);
  });
});

// Sessão demo que cai com 401 (reset dos dados): o cartão entra de novo no mesmo papel, uma vez por minuto.
describe('nova entrada automática depois do 401', () => {
  beforeEach(() => forgetDemoRole());

  it('depois de a sessão demo expirar, devolve o papel uma vez', () => {
    rememberDemoRole('porteiro');
    markSessionExpired();

    expect(takeAutoRetryRole(1_000_000)).toBe('porteiro');
    expect(takeAutoRetryRole(1_000_001)).toBeNull();
  });

  it('não repete dentro do mesmo minuto, mesmo que caia de novo', () => {
    rememberDemoRole('morador');
    markSessionExpired();
    expect(takeAutoRetryRole(1_000_000)).toBe('morador');

    markSessionExpired();
    expect(takeAutoRetryRole(1_030_000)).toBeNull();
    markSessionExpired();
    expect(takeAutoRetryRole(1_070_000)).toBe('morador');
  });

  it('sem sessão demo (login comum) não entra sozinho', () => {
    markSessionExpired();

    expect(takeAutoRetryRole(1_000_000)).toBeNull();
  });
});
