// Prints das telas no Chrome instalado (headless), para o README e para revisão visual.
// Precisa do painel (Vite) apontando para uma API LOCAL com o modo demonstração ligado.
//
//   node ferramentas/capturar-telas.mjs <url-do-painel> <pasta-destino> [url-do-app]
//   ex.: node ferramentas/capturar-telas.mjs http://localhost:5111 docs/img/prints http://localhost:5112
//
// Entra pelas contas de demonstração com o botão "Entrar como ..." (POST /api/demo/entrar, sem senha).
// Também confere cada tela: sem erro de JavaScript no console e sem rolagem horizontal no celular.
import { mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { chromium } from 'playwright-core';

const CHROME = process.env.CHROME_PATH || 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const [painel = 'http://localhost:5111', destino = 'docs/img/prints', app] = process.argv.slice(2);
const DESKTOP = { width: 1280, height: 800 };
const CELULAR = { width: 375, height: 812 };

const problemas = [];

async function abrir(contexto, url, esperar = 'networkidle') {
  const pagina = await contexto.newPage();
  const erros = [];
  pagina.on('pageerror', erro => erros.push(erro.message));
  pagina.on('console', msg => { if (msg.type() === 'error' && !/favicon|404 \(Not Found\)/.test(msg.text())) erros.push(msg.text()); });
  await pagina.goto(url, { waitUntil: esperar, timeout: 120000 });
  return { pagina, erros };
}

async function conferir(nome, pagina, erros) {
  const larguraExtra = await pagina.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  if (larguraExtra > 1) problemas.push(`${nome}: rolagem horizontal de ${larguraExtra}px`);
  if (erros.length) problemas.push(`${nome}: ${erros.join(' | ')}`);
}

async function print(nome, pagina, opcoes = {}) {
  await pagina.waitForTimeout(400);
  await pagina.screenshot({ path: join(destino, `${nome}.jpg`), type: 'jpeg', quality: 82, ...opcoes });
  console.log(`[prints] ${nome}.jpg`);
}

async function entrarComo(pagina, papel) {
  await pagina.getByRole('button', { name: new RegExp(`Entrar como ${papel}`) }).first().click();
  await pagina.waitForURL(/\/painel/);
  await pagina.waitForLoadState('networkidle');
}

mkdirSync(destino, { recursive: true });
const navegador = await chromium.launch({ executablePath: CHROME });

// Desktop (pule com SO_APP=1)
if (!process.env.SO_APP) {
  const contexto = await navegador.newContext({ viewport: DESKTOP, deviceScaleFactor: 1, reducedMotion: 'reduce' });
  let { pagina, erros } = await abrir(contexto, `${painel}/`);
  await pagina.getByRole('region', { name: /Explore a demonstração/ }).waitFor();
  await conferir('inicio', pagina, erros);
  await print('painel-inicio', pagina);
  await pagina.getByRole('region', { name: /Explore a demonstração/ }).screenshot({ path: join(destino, 'painel-demo.jpg'), type: 'jpeg', quality: 85 });
  console.log('[prints] painel-demo.jpg');

  ({ pagina, erros } = await abrir(contexto, `${painel}/entrar`));
  await pagina.getByRole('region', { name: /Explore a demonstração/ }).waitFor();
  await conferir('entrar', pagina, erros);
  await print('painel-entrar', pagina);

  await entrarComo(pagina, 'síndico');
  await pagina.getByRole('heading', { name: /Painel da gestão/ }).waitFor();
  await conferir('painel-gestao', pagina, erros);
  await print('painel-gestao', pagina);

  await pagina.goto(`${painel}/painel/reservas`, { waitUntil: 'networkidle' });
  await conferir('reservas', pagina, erros);
  await print('painel-reservas', pagina);

  await pagina.goto(`${painel}/sobre`, { waitUntil: 'networkidle' });
  await conferir('sobre', pagina, erros);
  await print('painel-sobre', pagina);

  await pagina.goto(`${painel}/caminho-que-nao-existe`, { waitUntil: 'networkidle' });
  await conferir('404', pagina, erros);
  await print('painel-404', pagina);
  await contexto.close();
}

// Celular: morador e porteiro
for (const [papel, arquivo, rota] of process.env.SO_APP ? [] : [['morador', 'painel-morador-celular', '/painel'], ['porteiro', 'painel-portaria-celular', '/painel']]) {
  const contexto = await navegador.newContext({ viewport: CELULAR, deviceScaleFactor: 2, isMobile: true, hasTouch: true, reducedMotion: 'reduce' });
  const { pagina, erros } = await abrir(contexto, `${painel}/entrar`);
  await pagina.getByRole('region', { name: /Explore a demonstração/ }).waitFor();
  await entrarComo(pagina, papel);
  await pagina.goto(`${painel}${rota}`, { waitUntil: 'networkidle' });
  await conferir(arquivo, pagina, erros);
  await print(arquivo, pagina);
  if (papel === 'morador') {
    await pagina.goto(`${painel}/painel/avisos`, { waitUntil: 'networkidle' });
    await conferir('avisos-celular', pagina, erros);
    await print('painel-avisos-celular', pagina);
  }
  await contexto.close();
}

// App (Expo web), se informado
if (app) {
  const contexto = await navegador.newContext({ viewport: CELULAR, deviceScaleFactor: 2, isMobile: true, hasTouch: true, reducedMotion: 'reduce' });
  // O servidor do Expo mantém uma conexão aberta (recarga): espera só o carregamento da página.
  const { pagina, erros } = await abrir(contexto, app, 'load');
  await pagina.getByText('Avisos do condomínio').first().waitFor({ timeout: 60000 });
  await print('app-onboarding', pagina);
  await pagina.getByText('Pular').click();
  await pagina.getByText('Explore a demonstração').waitFor({ timeout: 60000 });
  await conferir('app-login', pagina, erros);
  await print('app-login', pagina);
  await pagina.getByLabel('Entrar como morador').click();
  await pagina.getByText(/Olá, Maria/).waitFor({ timeout: 30000 });
  await print('app-inicio', pagina);
  await pagina.getByRole('link', { name: /Mais/ }).last().click();
  await pagina.getByLabel(/Financeiro/).click();
  await pagina.getByText('Em aberto').first().waitFor();
  await conferir('app-financeiro', pagina, erros);
  await print('app-financeiro', pagina);
  await contexto.close();
}

await navegador.close();
if (problemas.length) {
  console.log('[prints] PROBLEMAS:\n - ' + problemas.join('\n - '));
  process.exitCode = 1;
} else {
  console.log('[prints] sem erro de console e sem rolagem horizontal nas telas capturadas.');
}
