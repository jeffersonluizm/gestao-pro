// Dados extras de demonstração (para tirar prints e deixar os fluxos "vivos"), criados pela própria API.
// Roda contra uma API local com o modo demonstração ligado (Demo__Enabled=true): entra pelo papel em POST /api/demo/entrar.
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

// O seeder da API já cria: 3 avisos, a reserva da Maria no salão, 2 encomendas, o visitante, a ocorrência,
// a cobrança do mês e a assembleia. Aqui entra só o que ele não cria.
for (const [title, content, category] of [
  ['Coleta seletiva', 'Os novos contêineres de recicláveis estão ao lado da portaria. Papel, plástico e metal separados, por favor.', 'Geral'],
  ['Festa junina do condomínio', 'Sábado, a partir das 16h, na área da churrasqueira. Cada família leva um prato típico.', 'Evento'],
]) {
  await api('POST', '/api/announcements', T, { condominiumId, authorId: admin.userId, title, content, category })
}

await api('POST', '/api/reservations', pessoas.joao.token, { commonAreaId: areas['Churrasqueira'].id, userId: pessoas.joao.id, condominiumId, date: dia(9), startTime: '12:00', endTime: '16:00' })

// Cobrança do mês passado, já paga (o histórico do financeiro).
const anterior = await api('POST', '/api/Bills', T, { condominiumId, userId: pessoas.maria.id, description: 'Taxa condominial do mês anterior', amount: 620.0, dueDate: `${dia(-20)}T00:00:00Z` })
await api('POST', '/api/Payments', T, { billId: anterior.id, amountPaid: 620.0, paymentMethod: 'pix' })

console.log('[demo] pronto: morador extra, avisos, reserva e cobrança paga criados.')
