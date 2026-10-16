// Junta os resumos do k6 (antes e depois) numa tabela Markdown e em gráficos SVG versionáveis.
//   node relatorio.mjs <pasta-antes> <pasta-depois> <pasta-de-saida>
// Gera: tabela.md (todas as medições), req-s.svg e p95.svg (cenários comparáveis, 50 VUs).
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const [antesDir, depoisDir, saida] = process.argv.slice(2);
const COR = { antes: '#B07A28', depois: '#1A7FA3' }; // validadas (dataviz: CVD, contraste e croma)

function ler(dir, nome) {
  const arquivo = join(dir, `${nome}.json`);
  if (!existsSync(arquivo)) return null;
  const m = JSON.parse(readFileSync(arquivo, 'utf8')).metrics;
  const d = m.http_req_duration ?? {};
  const cont = n => m[n]?.count ?? 0;
  return {
    rps: m.http_reqs?.rate ?? 0,
    total: m.http_reqs?.count ?? 0,
    p50: d.med ?? 0, p95: d['p(95)'] ?? 0, p99: d['p(99)'] ?? 0,
    erro: (m.http_req_failed?.value ?? 0) * 100,
    ok2xx: cont('pedidos_2xx'), r4xx: cont('pedidos_4xx'), f5xx: cont('pedidos_5xx'),
    inexistente: m.login_email_inexistente?.med, senhaErrada: m.login_senha_errada?.med,
  };
}

const cenarios = [
  ['h1-login-10', 'H1 login, 10 VUs'], ['h1-login-50', 'H1 login, 50 VUs'],
  ['h2-avisos-10', 'H2 avisos, 10 VUs'], ['h2-avisos-50', 'H2 avisos, 50 VUs'], ['h2-avisos-100', 'H2 avisos, 100 VUs'], ['h2-avisos-200', 'H2 avisos, 200 VUs'],
  ['h3-avisos-com-ruido-10', 'H3 avisos + 20 mil de outro condomínio, 10 VUs'], ['h3-avisos-com-ruido-50', 'H3 avisos + ruído, 50 VUs'], ['h3-avisos-com-ruido-100', 'H3 avisos + ruído, 100 VUs'],
  ['h4-lista-publica-10', 'H4 lista de condomínios (cadastro), 10 VUs'], ['h4-lista-publica-20', 'H4 lista de condomínios, 20 VUs'], ['h4-lista-publica-50', 'H4 lista de condomínios, 50 VUs'],
  ['h5-escrita-50', 'H5 publicar aviso, 50 VUs'], ['h8-health-50', 'H8 health, 50 VUs'],
];

const fmt = (v, casas = 0) => (v === undefined || v === null ? '-' : Number(v).toLocaleString('pt-BR', { maximumFractionDigits: casas, minimumFractionDigits: casas }));
const linhas = ['| Cenário | Antes: req/s | Antes: p95 (ms) | Antes: erro | Depois: req/s | Depois: p95 (ms) | Depois: erro |', '|---|---:|---:|---:|---:|---:|---:|'];
const dados = [];
for (const [id, nome] of cenarios) {
  const a = ler(antesDir, id); const d = ler(depoisDir, id);
  if (!a && !d) continue;
  dados.push({ id, nome, a, d });
  linhas.push(`| ${nome} | ${fmt(a?.rps, 1)} | ${fmt(a?.p95, 1)} | ${fmt(a?.erro, 2)}% | ${fmt(d?.rps, 1)} | ${fmt(d?.p95, 1)} | ${fmt(d?.erro, 2)}% |`);
}
for (const [id, nome] of [['h6-reserva', 'H6 50 reservas simultâneas, mesmo horário'], ['h7-voto', 'H7 50 votos simultâneos, mesma pauta']]) {
  const a = ler(antesDir, id); const d = ler(depoisDir, id);
  const c = x => (x ? `${x.ok2xx} aceitos, ${x.r4xx} recusados (4xx), ${x.f5xx} com erro (5xx)` : '-');
  linhas.push(`| ${nome} | ${c(a)} | | | ${c(d)} | | |`);
}
const ta = ler(antesDir, 'h1b-tempo-login'); const td = ler(depoisDir, 'h1b-tempo-login');
const t = x => (x ? `inexistente ${fmt(x.inexistente, 1)} ms x senha errada ${fmt(x.senhaErrada, 1)} ms` : '-');
linhas.push(`| H1b mediana do login (1 VU) | ${t(ta)} | | | ${t(td)} | | |`);
writeFileSync(join(saida, 'tabela.md'), linhas.join('\n') + '\n');

// Gráfico de barras agrupadas (antes x depois), uma métrica por gráfico (nunca dois eixos).
function grafico(titulo, unidade, campo, ids, arquivo) {
  const itens = dados.filter(x => ids.includes(x.id));
  const largura = 760, alturaBarra = 18, gap = 2, bloco = alturaBarra * 2 + gap + 22, topo = 70, esquerda = 250, direita = 90;
  const altura = topo + itens.length * bloco + 40;
  const max = Math.max(1, ...itens.flatMap(x => [x.a?.[campo] ?? 0, x.d?.[campo] ?? 0]));
  const escala = v => ((largura - esquerda - direita) * v) / max;
  const partes = [];
  partes.push(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${largura} ${altura}" role="img" aria-labelledby="t d" font-family="Inter, Segoe UI, Arial, sans-serif">`);
  partes.push(`<title id="t">${titulo}</title><desc id="d">Barras agrupadas por cenário: antes (dourado) e depois (azul). Valores escritos ao lado de cada barra.</desc>`);
  partes.push(`<rect width="${largura}" height="${altura}" fill="#FFFFFF"/>`);
  partes.push(`<text x="24" y="30" font-size="17" font-weight="700" fill="#17212B">${titulo}</text>`);
  partes.push(`<rect x="24" y="44" width="12" height="12" rx="2" fill="${COR.antes}"/><text x="42" y="54" font-size="12" fill="#55626F">antes (main)</text>`);
  partes.push(`<rect x="140" y="44" width="12" height="12" rx="2" fill="${COR.depois}"/><text x="158" y="54" font-size="12" fill="#55626F">depois (feat/portfolio-ui-ux)</text>`);
  itens.forEach((x, i) => {
    const y = topo + i * bloco;
    partes.push(`<text x="${esquerda - 10}" y="${y + alturaBarra + 4}" font-size="12" text-anchor="end" fill="#17212B">${x.nome}</text>`);
    [['antes', x.a], ['depois', x.d]].forEach(([k, v], j) => {
      const val = v?.[campo] ?? 0; const w = Math.max(2, escala(val)); const yy = y + j * (alturaBarra + gap);
      partes.push(`<rect x="${esquerda}" y="${yy}" width="${w}" height="${alturaBarra}" rx="4" fill="${COR[k]}"><title>${x.nome}, ${k}: ${fmt(val, 1)} ${unidade}</title></rect>`);
      partes.push(`<text x="${esquerda + w + 6}" y="${yy + 13}" font-size="11" fill="#55626F">${fmt(val, 1)} ${unidade}</text>`);
    });
  });
  partes.push(`<line x1="${esquerda}" y1="${topo - 6}" x2="${esquerda}" y2="${altura - 30}" stroke="#DCE4EA"/>`);
  partes.push(`<text x="24" y="${altura - 10}" font-size="11" fill="#55626F">Mesma máquina para gerador (k6), API e banco: compare antes x depois, não leia como capacidade de produção.</text>`);
  partes.push('</svg>');
  writeFileSync(join(saida, arquivo), partes.join('\n') + '\n');
}

const comparaveis = ['h2-avisos-50', 'h3-avisos-com-ruido-50', 'h4-lista-publica-20', 'h5-escrita-50', 'h8-health-50', 'h1-login-50'];
grafico('Requisições por segundo (maior é melhor)', 'req/s', 'rps', comparaveis, 'req-s.svg');
grafico('Latência p95 (menor é melhor)', 'ms', 'p95', comparaveis, 'p95.svg');
console.log(`[relatorio] ${dados.length} cenários; tabela.md, req-s.svg e p95.svg em ${saida}`);
