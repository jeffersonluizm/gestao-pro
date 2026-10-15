// Dados extras de demonstração (para tirar prints e deixar os fluxos "vivos"), criados pela própria API.
// Roda contra uma API local com o modo demonstração ligado (Demo__Enabled=true): as credenciais vêm de GET /api/demo.
//
//   API=http://127.0.0.1:5088 node tests/demo/semear-demo.mjs
const API = process.env.API ?? 'http://127.0.0.1:5088'

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

// Entrada pelo papel (POST /api/demo/entrar): as contas demo não têm senha.
const entrar = async role => {
  const login = await api('POST', '/api/demo/entrar', null, { papel: role })
  return { ...login, token: login.token, id: login.userId }
}

const dia = offset => new Date(Date.now() + offset * 86400000).toISOString().slice(0, 10)

const admin = await entrar('sindico')
const T = admin.token
const condominiumId = admin.condominiumId

const areas = Object.fromEntries((await api('GET', '/api/CommonAreas', T)).map(area => [area.name, area]))
const unidades = await api('GET', `/api/Units/condominium/${condominiumId}`, T)
const unit101 = unidades.find(u => u.number === '101')
const unit102 = unidades.find(u => u.number === '102') ?? await api('POST', '/api/Units', T, { condominiumId, number: '102', block: 'A', floor: 1, type: 'apt' })

const pessoas = { maria: await entrar('morador'), portaria: await entrar('porteiro') }
// Morador extra (fora das contas demo protegidas), com troca de senha do primeiro acesso.
const INICIAL = 'Senha-Inicial-2026'
const SENHA = 'Senha-Do-Joao-2026'
const joao = await api('POST', '/api/Users', T, {
  condominiumId, unitId: unit102.id, name: 'João Lima', email: 'joao@exemplo.gestaopro.local', password: INICIAL,
  phone: '3133334444', cpf: '44444444444', role: 'morador',
})
await api('PATCH', '/api/Auth/change-password', null, { userId: joao.id, currentPassword: INICIAL, newPassword: SENHA })
pessoas.joao = { ...joao, token: (await api('POST', '/api/Auth/login', null, { registrationNumber: 'joao@exemplo.gestaopro.local', password: SENHA })).token }

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
  await api('POST', '/api/Delivery', pessoas.portaria.token, { trackingCode, type: 'encomenda', description, residentId: pessoas.maria.id, unitId: unit101.id, status: 'pending' })
}

const visitante = await api('POST', '/api/Visitor/register', pessoas.portaria.token, {
  fullName: 'Paulo Mendes', document: '12.345.678-9', phone: '3199998888', residentId: pessoas.maria.id, condominiumId, unitId: unit101.id, reason: 'Visita',
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

console.log('[demo] pronto: reservas, encomendas, visitante, ocorrência, cobranças e assembleia criadas.')
