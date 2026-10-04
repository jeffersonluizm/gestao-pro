// Massa de dados do teste de estresse, SEMPRE num banco descartável.
//
// Uso:
//   API=http://127.0.0.1:5101 MONGO=mongodb://127.0.0.1:27100 SAIDA=estado.json node semear.mjs base
//   API=... MONGO=... node semear.mjs ruido 20000      (avisos de OUTRO condomínio, para H3)
//
// "base": usa a API (conta de síndico do modo demonstração, lida de GET /api/demo) para criar área comum,
// 100 avisos e uma assembleia; depois, direto no MongoDB, clona o condomínio de demonstração e os
// moradores mock para chegar a 50 condomínios x 100 usuários (H4). O banco PRECISA ser GestaoProStress
// em 127.0.0.1:27100: qualquer outra coisa aborta.
import { writeFileSync } from 'node:fs';
import { MongoClient, ObjectId } from 'mongodb';

const API = process.env.API ?? 'http://127.0.0.1:5101';
const MONGO = process.env.MONGO ?? 'mongodb://127.0.0.1:27100';
const BANCO = 'GestaoProStress';

if (!/^mongodb:\/\/(127\.0\.0\.1|localhost):27100\/?$/.test(MONGO)) {
  console.error(`[semear] recusado: MONGO precisa ser o banco descartável em 127.0.0.1:27100 (recebi ${MONGO}).`);
  process.exit(1);
}

async function portao() {
  const r = await fetch(`${API}/api/health/mongodb`);
  const corpo = await r.json();
  if (r.status !== 200 || corpo.database !== BANCO) {
    console.error(`[semear] BLOQUEADO pelo portão: banco "${corpo.database}".`);
    process.exit(1);
  }
}

async function api(method, path, token, body) {
  const r = await fetch(`${API}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  });
  const texto = await r.text();
  if (!r.ok) throw new Error(`${method} ${path} -> ${r.status} ${texto.slice(0, 200)}`);
  return texto ? JSON.parse(texto) : undefined;
}

async function base() {
  // Credenciais: as do modo demonstração (GET /api/demo). Builds antigos (linha de base, sem o modo demo)
  // recebem a conta pelo ambiente: CONTA_CARGA_EMAIL e CONTA_CARGA_SENHA (nada fixo no código).
  const demo = await fetch(`${API}/api/demo`).then(r => (r.ok ? r.json() : null))
  const sindico = demo?.accounts.find(account => account.role === 'sindico')
    ?? { email: process.env.CONTA_CARGA_EMAIL, password: process.env.CONTA_CARGA_SENHA };
  if (!sindico.email || !sindico.password) throw new Error('Sem /api/demo: defina CONTA_CARGA_EMAIL e CONTA_CARGA_SENHA.');
  const login = await api('POST', '/api/Auth/login', null, { registrationNumber: sindico.email, password: sindico.password });
  const token = login.token;
  const condominiumId = login.condominiumId;

  const area = await api('POST', '/api/CommonAreas', token, {
    condominiumId, name: 'Salão de festas (carga)', description: 'Área da carga', capacity: 80,
    openTime: '08:00', closeTime: '22:00', rules: '',
  });

  for (let i = 0; i < 100; i++) {
    await api('POST', '/api/announcements', token, {
      condominiumId, authorId: login.userId, title: `Aviso ${i + 1}`, content: 'Conteúdo do aviso de carga.', category: 'Geral',
    });
  }

  const inicio = new Date();
  const fim = new Date(Date.now() + 7 * 24 * 3600 * 1000);
  const assembleia = await api('POST', '/api/assembly', token, {
    condominiumId, createdBy: login.userId, title: 'Assembleia de carga', description: 'Votação da carga',
    startDate: inicio.toISOString(), endDate: fim.toISOString(), scheduledDate: inicio.toISOString(),
    topics: [{ title: 'Pauta 1', desc: '', voteOptions: ['sim', 'não'] }],
  });

  // 50 condomínios x 100 usuários (H4), clonando os documentos que a própria API criou.
  const cliente = new MongoClient(MONGO);
  await cliente.connect();
  const db = cliente.db(BANCO);
  const modeloCondominio = await db.collection('condominiums').findOne({ _id: new ObjectId(condominiumId) });
  const modeloUsuario = await db.collection('users').findOne({ CondominiumId: new ObjectId(condominiumId), Role: { $ne: 'admin' } })
    ?? await db.collection('users').findOne({ CondominiumId: new ObjectId(condominiumId) });
  const outros = [];
  for (let c = 1; c < 50; c++) {
    const id = new ObjectId();
    outros.push(id.toHexString());
    const { _id, ...resto } = modeloCondominio;
    await db.collection('condominiums').insertOne({ ...resto, _id: id, Name: `Condomínio de carga ${c}`, Cnpj: `99.${String(c).padStart(3, '0')}.000/0001-00` });
  }
  const todos = [condominiumId, ...outros];
  const usuarios = [];
  for (const [c, cid] of todos.entries()) {
    for (let u = 0; u < 100; u++) {
      const { _id, ...resto } = modeloUsuario;
      const n = `${c}-${u}`;
      usuarios.push({
        ...resto, _id: new ObjectId(), CondominiumId: new ObjectId(cid), Name: `Morador ${n}`,
        Email: `morador-${n}@carga.local`, RegistrationNumber: `CARGA-${n}`, Cpf: String(90000000000 + c * 1000 + u),
        Role: 'morador', IsActive: true,
      });
    }
  }
  await db.collection('users').insertMany(usuarios);
  await cliente.close();

  const estado = { api: API, token, email: sindico.email, senha: sindico.password, condominiumId, userId: login.userId, commonAreaId: area.id, assemblyId: assembleia.id, outroCondominioId: outros[0] };
  writeFileSync(process.env.SAIDA ?? 'estado.json', JSON.stringify(estado, null, 2));
  console.log(`[semear] base pronta: 100 avisos, 50 condomínios, ${usuarios.length} usuários.`);
}

async function ruido(quantidade) {
  const cliente = new MongoClient(MONGO);
  await cliente.connect();
  const db = cliente.db(BANCO);
  const modelo = await db.collection('announcements').findOne({});
  const outro = (await db.collection('condominiums').find({}).sort({ _id: -1 }).limit(1).next())._id;
  const lote = [];
  for (let i = 0; i < quantidade; i++) {
    const { _id, ...resto } = modelo;
    lote.push({ ...resto, _id: new ObjectId(), condominiumId: outro, title: `Ruído ${i}`, createdAt: new Date(Date.now() - i * 1000) });
  }
  await db.collection('announcements').insertMany(lote);
  console.log(`[semear] ${quantidade} avisos de outro condomínio inseridos (total ${await db.collection('announcements').countDocuments()}).`);
  await cliente.close();
}

await portao();
const [modo, arg] = process.argv.slice(2);
if (modo === 'base') await base();
else if (modo === 'ruido') await ruido(Number(arg ?? 20000));
else { console.error('uso: node semear.mjs base | ruido <n>'); process.exit(1); }
