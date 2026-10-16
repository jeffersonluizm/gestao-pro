// H5: escrita sob carga (POST de aviso pela administração).
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURACAO || '60s',
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const params = { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${__ENV.TOKEN}` } };

export default function () {
  const body = JSON.stringify({ condominiumId: __ENV.CONDOMINIO, authorId: __ENV.USUARIO, title: `Carga ${__VU}-${__ITER}`, content: 'x', category: 'Geral' });
  const r = http.post(`${__ENV.API}/api/announcements`, body, params);
  check(r, { 'status 2xx': res => res.status >= 200 && res.status < 300 });
}
