// H1b: tempo de resposta do login para e-mail inexistente x senha errada (1 VU, sequencial, alternando).
import http from 'k6/http';
import { Trend } from 'k6/metrics';

export const options = { vus: 1, iterations: Number(__ENV.ITERACOES || 200), summaryTrendStats: ['avg', 'med', 'p(95)'] };

const inexistente = new Trend('login_email_inexistente', true);
const senhaErrada = new Trend('login_senha_errada', true);
const params = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const a = http.post(`${__ENV.API}/api/Auth/login`, JSON.stringify({ registrationNumber: `ninguem-${__ITER}@carga.local`, password: 'Senha-Qualquer-2026' }), params);
  inexistente.add(a.timings.duration);
  const b = http.post(`${__ENV.API}/api/Auth/login`, JSON.stringify({ registrationNumber: 'admin@gestaopro.local', password: 'Senha-Errada-2026' }), params);
  senhaErrada.add(b.timings.duration);
}
