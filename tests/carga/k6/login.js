// Login sob carga (BCrypt): exige o limite de autenticação alto (RateLimiting__Auth__PermitLimit).
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURACAO || '60s',
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const body = JSON.stringify({ registrationNumber: 'admin@gestaopro.local', password: 'Admin@123' });
const params = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const r = http.post(`${__ENV.API}/api/Auth/login`, body, params);
  check(r, { 'status 200': res => res.status === 200 });
}
