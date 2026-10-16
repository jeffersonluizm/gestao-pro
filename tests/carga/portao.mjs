// Portão de segurança: antes de QUALQUER requisição de carga, confirma que a API está ligada
// ao banco descartável. Se o health não disser "GestaoProStress", aborta com código 1.
const base = process.env.API ?? 'http://127.0.0.1:5101';
const esperado = process.env.BANCO_ESPERADO ?? 'GestaoProStress';

const resposta = await fetch(`${base}/api/health/mongodb`).catch(error => {
  console.error(`[portao] API inacessível em ${base}: ${error.message}`);
  process.exit(1);
});
const corpo = await resposta.json().catch(() => ({}));

if (resposta.status !== 200 || corpo.connected !== true || corpo.database !== esperado) {
  console.error(`[portao] BLOQUEADO: status ${resposta.status}, banco "${corpo.database}" (esperado "${esperado}").`);
  process.exit(1);
}
console.log(`[portao] ok: ${base} usa o banco ${corpo.database}.`);
