// H6/H7: N pedidos ao mesmo tempo (1 por VU), mesmo corpo. Conta sucessos e recusas por faixa de status.
// TIPO=reserva: mesmo horário da mesma área. TIPO=voto: mesmo usuário, mesma pauta.
import http from 'k6/http';
import { Counter } from 'k6/metrics';

const n = Number(__ENV.N || 50);
export const options = { scenarios: { juntos: { executor: 'per-vu-iterations', vus: n, iterations: 1, maxDuration: '60s' } } };

const sucesso = new Counter('pedidos_2xx');
const recusa = new Counter('pedidos_4xx');
const falha = new Counter('pedidos_5xx');
const params = { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${__ENV.TOKEN}` } };

export default function () {
  const r = __ENV.TIPO === 'voto'
    ? http.post(`${__ENV.API}/api/vote`, JSON.stringify({ assemblyId: __ENV.ASSEMBLEIA, topicIndex: 0, vote: 'sim' }), params)
    : http.post(`${__ENV.API}/api/reservations`, JSON.stringify({
      commonAreaId: __ENV.AREA, userId: __ENV.USUARIO, condominiumId: __ENV.CONDOMINIO,
      date: __ENV.DATA, startTime: '18:00', endTime: '20:00', status: 'pendente',
    }), params);
  if (r.status >= 500) falha.add(1);
  else if (r.status >= 400) recusa.add(1);
  else sucesso.add(1);
}
