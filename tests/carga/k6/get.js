// Leitura sob carga: GET em CAMINHO com o token do estado (ou anônimo se ANONIMO=1).
// k6 run -e API=... -e TOKEN=... -e CAMINHO=/api/announcements -e VUS=50 -e DURACAO=60s get.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURACAO || '60s',
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const params = __ENV.ANONIMO === '1' ? {} : { headers: { Authorization: `Bearer ${__ENV.TOKEN}` } };

export default function () {
  const r = http.get(`${__ENV.API}${__ENV.CAMINHO}`, params);
  check(r, { 'status 200': res => res.status === 200 });
}
