// Dados de demonstração realistas (para conhecer o sistema e tirar prints), criados pela própria API.
// Só roda contra uma API em localhost que já tenha a conta de demonstração (ambiente Development).
//
//   API=http://127.0.0.1:5088 node tests/demo/semear-demo.mjs
//
// Contas criadas (senha de todas: Demo-Gestao-2026):
//   sindica@demo.gestaopro.local (síndica), portaria@demo.gestaopro.local (portaria),
//   maria@demo.gestaopro.local e joao@demo.gestaopro.local (moradores).
const API = process.env.API ?? 'http://127.0.0.1:5088'
const SENHA = 'Demo-Gestao-2026'
const INICIAL = 'Senha-Inicial-2026'

if (!/^http:\/\/(127\.0\.0\.1|localhost):\d+$/.test(API)) {
  console.error(`[demo] recusado: use uma API local (recebi ${API}).`)
  process.exit(1)
}

async function api(method, path, token, body) {
  const r = await fetch(`${API}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  })
  const text = await r.text()
  if (!r.ok) throw new Error(`${method} ${path} -> ${r.status} ${text.slice(0, 300)}`)
  return text ? JSON.parse(text) : undefined
}

const dia = offset => new Date(Date.now() + offset * 86400000).toISOString().slice(0, 10)

const admin = await api('POST', '/api/Auth/login', null, { registrationNumber: 'admin@gestaopro.local', password: 'Admin@123' })
const T = admin.token
const condominiumId = admin.condominiumId

const areas = {}
for (const [name, capacity, openTime, closeTime, description] of [
  ['Salão de festas', 80, '10:00', '23:00', 'Salão com cozinha, mesas para 80 pessoas e som ambiente.'],
  ['Churrasqueira', 30, '10:00', '22:00', 'Área coberta com duas churrasqueiras e pia.'],
  ['Quadra poliesportiva', 20, '08:00', '21:00', 'Quadra com iluminação, rede de vôlei e tabelas de basquete.'],
]) {
  areas[name] = await api('POST', '/api/CommonAreas', T, { condominiumId, name, description, capacity, openTime, closeTime, rules: 'Deixar o espaço limpo ao final do uso.' })
}

const units = {}
for (const number of ['101', '102', '201', '202']) {
  units[number] = await api('POST', '/api/Units', T, { condominiumId, number, block: 'A', floor: Number(number[0]), type: 'apt' })
}

const pessoas = {}
for (const [key, name, email, role, unit, cpf, phone] of [
  ['sindica', 'Ana Ribeiro', 'sindica@demo.gestaopro.local', 'sindico', '201', '41111111111', '3133331111'],
  ['portaria', 'Carlos Pereira', 'portaria@demo.gestaopro.local', 'porteiro', null, '42222222222', '3133332222'],
  ['maria', 'Maria Souza', 'maria@demo.gestaopro.local', 'morador', '101', '43333333333', '3133333333'],
  ['joao', 'João Lima', 'joao@demo.gestaopro.local', 'morador', '102', '44444444444', '3133334444'],
]) {
  const created = await api('POST', '/api/Users', T, {
    condominiumId, unitId: unit ? units[unit].id : undefined, name, email, password: INICIAL, phone, cpf, role,
  })
  // Primeiro acesso: troca a senha inicial pela senha de demonstração.
  await api('PATCH', '/api/Auth/change-password', null, { userId: created.id, currentPassword: INICIAL, newPassword: SENHA })
  const login = await api('POST', '/api/Auth/login', null, { registrationNumber: email, password: SENHA })
  pessoas[key] = { ...created, token: login.token }
}

for (const [title, content, category] of [
  ['Manutenção do elevador social', 'Na sexta-feira, das 9h às 12h, o elevador social fica parado para manutenção preventiva. Use o elevador de serviço.', 'Manutenção'],
  ['Assembleia geral ordinária', 'Dia 20, às 19h, no salão de festas. Pauta: previsão orçamentária e troca do portão da garagem. A votação também fica aberta no sistema.', 'Assembleia'],
  ['Coleta seletiva', 'Os novos contêineres de recicláveis estão ao lado da portaria. Papel, plástico e metal separados, por favor.', 'Geral'],
  ['Festa junina do condomínio', 'Sábado, a partir das 16h, na área da churrasqueira. Cada família leva um prato típico.', 'Evento'],
]) {
  await api('POST', '/api/announcements', T, { condominiumId, authorId: admin.userId, title, content, category })
}

await api('POST', '/api/reservations', pessoas.maria.token, { commonAreaId: areas['Salão de festas'].id, userId: pessoas.maria.id, condominiumId, date: dia(6), startTime: '14:00', endTime: '18:00', observations: 'Aniversário da Laura' })
await api('POST', '/api/reservations', pessoas.joao.token, { commonAreaId: areas['Churrasqueira'].id, userId: pessoas.joao.id, condominiumId, date: dia(9), startTime: '12:00', endTime: '16:00' })

for (const [trackingCode, description] of [['BR123456789', 'Caixa média, Loja Online'], ['LX998877', 'Envelope de documentos']]) {
  await api('POST', '/api/Delivery', pessoas.portaria.token, { trackingCode, type: 'encomenda', description, residentId: pessoas.maria.id, unitId: units['101'].id, status: 'pending' })
}

const visitante = await api('POST', '/api/Visitor/register', pessoas.portaria.token, {
  fullName: 'Paulo Mendes', document: '12.345.678-9', phone: '3199998888', residentId: pessoas.maria.id, condominiumId, unitId: units['101'].id, reason: 'Visita',
})
await api('POST', `/api/Visitor/check-in/${visitante.id}`, pessoas.portaria.token)

await api('POST', '/api/Occurrences', pessoas.maria.token, { condominiumId, userId: pessoas.maria.id, title: 'Vazamento na garagem', description: 'Goteira perto da vaga 12, no subsolo.', category: 'Maintenance', priority: 'Medium' })

await api('POST', '/api/Bills', T, { condominiumId, userId: pessoas.maria.id, description: 'Taxa condominial 10/2026', amount: 620.0, dueDate: `${dia(10)}T00:00:00Z` })
const setembro = await api('POST', '/api/Bills', T, { condominiumId, userId: pessoas.maria.id, description: 'Taxa condominial 09/2026', amount: 620.0, dueDate: `${dia(-20)}T00:00:00Z` })
await api('POST', '/api/Payments', T, { billId: setembro.id, amountPaid: 620.0, paymentMethod: 'pix' })

const inicio = new Date()
const assembleia = await api('POST', '/api/assembly', T, {
  condominiumId, createdBy: admin.userId, title: 'Assembleia geral ordinária', description: 'Previsão orçamentária e obras.',
  startDate: inicio.toISOString(), endDate: new Date(Date.now() + 14 * 86400000).toISOString(), scheduledDate: inicio.toISOString(),
  topics: [
    { title: 'Aprovar a previsão orçamentária de 2027', desc: '', voteOptions: ['sim', 'não'] },
    { title: 'Trocar o portão da garagem', desc: '', voteOptions: ['sim', 'não'] },
  ],
})
await api('POST', '/api/vote', pessoas.maria.token, { assemblyId: assembleia.id, topicIndex: 0, vote: 'sim' })

console.log('[demo] pronto. Contas: sindica@, portaria@, maria@, joao@demo.gestaopro.local, senha', SENHA)
