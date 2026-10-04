// Espelho da PasswordPolicy da API: de 8 a 128 caracteres e não óbvia.
// O app só avisa antes de enviar; quem decide é a API.

export const WEAK_PASSWORD_MESSAGE =
  'A senha precisa ter de 8 a 128 caracteres e não pode ser óbvia (como 12345678, senha123 ou letras repetidas).';

const COMMON = new Set([
  '123456', '1234567', '12345678', '123456789', '1234567890', '87654321', 'password', 'senha123',
  'senha1234', 'qwerty12', 'qwerty123', 'qwertyui', 'abc12345', 'admin123', 'condominio', '12341234',
]);

function isSequence(value: string) {
  const lower = value.toLowerCase();
  let ascending = true;
  let descending = true;
  for (let i = 1; i < lower.length; i++) {
    const step = lower.charCodeAt(i) - lower.charCodeAt(i - 1);
    ascending &&= step === 1;
    descending &&= step === -1;
  }
  return ascending || descending;
}

export function isStrongPassword(password: string) {
  if (password.length > 128) return false;
  const value = password.trim();
  if (value.length < 8) return false;
  if (COMMON.has(value.toLowerCase())) return false;
  if (new Set(value).size < 4) return false;
  return !isSequence(value);
}
