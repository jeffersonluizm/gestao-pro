import { describe, expect, it } from 'vitest'
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from './passwordPolicy';

// Mesma regra da API (PasswordPolicy): o app avisa antes de enviar, a API decide.
describe('isStrongPassword', () => {
  it.each(['', 'Ab1-xyz', '12345678', 'senha123', 'aaaaaaaa', 'abcdefgh', 'x'.repeat(129)])(
    'recusa senha curta, longa ou óbvia: %s',
    password => expect(isStrongPassword(password)).toBe(false),
  );

  it.each(['senha-nova', 'Jardim-Azul-2026'])('aceita senha razoável: %s', password => {
    expect(isStrongPassword(password)).toBe(true);
  });

  it('explica a regra em português', () => {
    expect(WEAK_PASSWORD_MESSAGE).toContain('8 a 128 caracteres');
  });
});
