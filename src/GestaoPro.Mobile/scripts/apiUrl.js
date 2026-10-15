// Regras de build do app (usadas pelo app.config.js). Ver docs/deploy.md.

// EXPO_PUBLIC_API_URL é obrigatória no export de produção (Expo web ou build nativo): sem ela o app procuraria a
// API em http://localhost:5088. Precisa ser https, exceto localhost para testar na máquina.
function apiUrlProblem(value) {
  const url = (value || '').trim();
  if (!url) {
    return 'EXPO_PUBLIC_API_URL não definida. O export de produção precisa do endereço da API, por exemplo ' +
      'EXPO_PUBLIC_API_URL=https://gestaopro-api.onrender.com (ver docs/deploy.md).';
  }
  let parsed;
  try {
    parsed = new URL(url);
  } catch {
    return `EXPO_PUBLIC_API_URL inválida ("${url}"). Use o endereço completo com https://.`;
  }
  const local = ['localhost', '127.0.0.1', '[::1]'].includes(parsed.hostname);
  if (parsed.protocol !== 'https:' && !(local && parsed.protocol === 'http:')) {
    return `EXPO_PUBLIC_API_URL precisa usar https ("${url}"): o navegador e o Android bloqueiam chamadas http em produção.`;
  }
  return null;
}

// Desenvolvimento (expo start): http liberado, para o celular falar com a API no IP do PC.
// Produção (expo export, build nativo): URL https obrigatória e http bloqueado, salvo GESTAOPRO_PERMITIR_HTTP=1
// num build de teste na rede local.
function buildSettings(env) {
  const production = env.NODE_ENV === 'production';
  const allowHttp = env.GESTAOPRO_PERMITIR_HTTP === '1';
  if (!production) return { cleartext: true, problem: null };
  if (allowHttp) return { cleartext: true, problem: env.EXPO_PUBLIC_API_URL ? null : apiUrlProblem(undefined) };
  return { cleartext: false, problem: apiUrlProblem(env.EXPO_PUBLIC_API_URL) };
}

module.exports = { apiUrlProblem, buildSettings };
