import { ReactNode, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { requestJson } from '../api/http'
import { useAuth } from '../auth/AuthContext'
import { isFrontDesk } from '../auth/session'
import { EmptyState } from '../components/ui/States'
import { Bill, Delivery, Occurrence, Reservation, Row, Visitor } from '../domain/types'
import { AnnouncementsFeed } from '../features/announcements/AnnouncementsFeed'
import { formatCurrency, formatDateOnly } from '../lib/format'
import { todayInputValue } from '../lib/masks'

type Metric = number | string | null

// Busca uma lista; em erro devolve null (o cartão mostra "indisponível" sem derrubar o painel).
function useList<T>(path: string | null) {
  const [data, setData] = useState<T[] | null>(null)
  const [failed, setFailed] = useState(false)
  useEffect(() => {
    if (!path) return
    let active = true
    requestJson<T[]>(path)
      .then(result => { if (active) setData(Array.isArray(result) ? result : []) })
      .catch(() => { if (active) setFailed(true) })
    return () => { active = false }
  }, [path])
  return { data, failed }
}

function MetricCard({ to, label, value, failed, hint }: { to: string; label: string; value: Metric; failed?: boolean; hint?: string }) {
  return (
    <Link className="metric-card" to={to} aria-label={`${label}: ${failed ? 'indisponível' : value ?? 'carregando'}`}>
      <span>{label}</span>
      {failed ? <strong aria-hidden>-</strong> : value === null ? <div className="skeleton" style={{ height: 32, width: 64 }} /> : <strong>{value}</strong>}
      {hint && <span>{hint}</span>}
    </Link>
  )
}

function Heading({ eyebrow, title, children }: { eyebrow: string; title: string; children?: ReactNode }) {
  return (
    <header className="page-heading">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1>{title}</h1>
        {children && <p>{children}</p>}
      </div>
    </header>
  )
}

function ManagerDashboard() {
  const { session } = useAuth()
  const condominium = encodeURIComponent(session.condominiumId)
  const users = useList<Row>('/api/Users')
  const reservations = useList<Reservation>('/api/reservations')
  const occurrences = useList<Occurrence>(`/api/Occurrences/condominium/${condominium}`)
  const deliveries = useList<Delivery>('/api/Delivery')
  const visitors = useList<Visitor>('/api/Visitor')
  const pending = reservations.data?.filter(item => item.status === 'pendente') ?? []
  const openOccurrences = occurrences.data?.filter(item => item.status === 'Open' || item.status === 'InProgress') ?? []

  return (
    <>
      <Heading eyebrow="Gestão" title="Painel da gestão">O que precisa de atenção no condomínio hoje.</Heading>
      <section className="dashboard-grid" aria-label="Indicadores">
        <MetricCard to="/painel/usuarios" label="Moradores ativos" value={users.data ? users.data.filter(user => user.role === 'morador').length : null} failed={users.failed} />
        <MetricCard to="/painel/reservas" label="Reservas pendentes" value={reservations.data ? pending.length : null} failed={reservations.failed} />
        <MetricCard to="/painel/ocorrencias" label="Ocorrências abertas" value={occurrences.data ? openOccurrences.length : null} failed={occurrences.failed} />
        <MetricCard to="/painel/encomendas" label="Encomendas aguardando" value={deliveries.data ? deliveries.data.filter(item => item.status === 'pending').length : null} failed={deliveries.failed} />
        <MetricCard to="/painel/visitantes" label="Visitantes no prédio" value={visitors.data ? visitors.data.filter(item => item.status === 'checked-in').length : null} failed={visitors.failed} />
      </section>
      <div className="section-grid">
        <section className="panel" aria-labelledby="pendentes">
          <h2 id="pendentes">Reservas aguardando aprovação</h2>
          {!pending.length
            ? <EmptyState title="Nenhuma reserva pendente." />
            : (
              <ul className="list-plain">
                {pending.slice(0, 5).map(item => (
                  <li key={item.id}><span>{formatDateOnly(item.date)} · {item.startTime} às {item.endTime}</span><Link to="/painel/reservas">Ver</Link></li>
                ))}
              </ul>
            )}
        </section>
        <section className="panel" aria-labelledby="ocorrencias">
          <h2 id="ocorrencias">Ocorrências abertas</h2>
          {!openOccurrences.length
            ? <EmptyState title="Nenhuma ocorrência aberta." />
            : (
              <ul className="list-plain">
                {openOccurrences.slice(0, 5).map(item => (
                  <li key={item.id}><span>{item.title}</span><Link to="/painel/ocorrencias">Tratar</Link></li>
                ))}
              </ul>
            )}
        </section>
      </div>
    </>
  )
}

function ResidentDashboard() {
  const { session } = useAuth()
  const user = encodeURIComponent(session.userId)
  const deliveries = useList<Delivery>('/api/Delivery')
  const bills = useList<Bill>(`/api/Bills/user/${user}`)
  const reservations = useList<Reservation>(`/api/reservations/user/${user}`)
  const openBills = bills.data?.filter(bill => bill.status === 'pending' || bill.status === 'overdue') ?? []
  const today = todayInputValue()
  const next = reservations.data
    ?.filter(item => item.date.slice(0, 10) >= today && !['cancelada', 'rejeitada'].includes(item.status))
    .sort((a, b) => `${a.date}${a.startTime}`.localeCompare(`${b.date}${b.startTime}`))[0]
  const firstName = (session.userName || 'morador').split(' ')[0]

  return (
    <>
      <Heading eyebrow="Meu condomínio" title={`Olá, ${firstName}`}>Suas encomendas, cobranças, reservas e os avisos mais recentes.</Heading>
      <section className="dashboard-grid" aria-label="Resumo">
        <MetricCard to="/painel/encomendas" label="Encomendas aguardando" value={deliveries.data ? deliveries.data.filter(item => item.status === 'pending').length : null} failed={deliveries.failed} hint="Retire na portaria" />
        <MetricCard to="/painel/cobrancas" label="Cobranças em aberto" value={bills.data ? openBills.length : null} failed={bills.failed} hint={openBills.length ? formatCurrency(openBills.reduce((sum, bill) => sum + Number(bill.amount ?? 0), 0)) : undefined} />
        <MetricCard to="/painel/reservas" label="Próxima reserva" value={reservations.data ? (next ? formatDateOnly(next.date) : 'Nenhuma') : null} failed={reservations.failed} hint={next ? `${next.startTime} às ${next.endTime}` : 'Reserve uma área comum'} />
      </section>
      <section className="section-grid" aria-labelledby="avisos-recentes" style={{ gridTemplateColumns: '1fr' }}>
        <div>
          <div className="panel-header">
            <h2 id="avisos-recentes">Avisos recentes</h2>
            <Link to="/painel/avisos">Ver todos</Link>
          </div>
          <AnnouncementsFeed limit={3} />
        </div>
      </section>
    </>
  )
}

function FrontDeskDashboard() {
  const deliveries = useList<Delivery>('/api/Delivery')
  const visitors = useList<Visitor>('/api/Visitor')
  const inside = visitors.data?.filter(item => item.status === 'checked-in') ?? []

  return (
    <>
      <Heading eyebrow="Portaria" title="Portaria">Quem está no prédio e o que falta entregar.</Heading>
      <section className="dashboard-grid" aria-label="Portaria">
        <MetricCard to="/painel/visitantes" label="Visitantes no prédio" value={visitors.data ? inside.length : null} failed={visitors.failed} hint="Registrar entrada e saída" />
        <MetricCard to="/painel/encomendas" label="Encomendas a entregar" value={deliveries.data ? deliveries.data.filter(item => item.status === 'pending').length : null} failed={deliveries.failed} hint="Registrar chegada e retirada" />
      </section>
      <section className="panel" style={{ marginTop: 24 }} aria-labelledby="no-predio">
        <h2 id="no-predio">No prédio agora</h2>
        {!inside.length
          ? <EmptyState title="Nenhum visitante dentro do prédio." />
          : <ul className="list-plain">{inside.map(item => <li key={item.id}><span>{item.fullName}</span><Link to="/painel/visitantes">Registrar saída</Link></li>)}</ul>}
      </section>
    </>
  )
}

// /painel — o resumo muda conforme o papel (e a visão escolhida pelo síndico).
export function DashboardPage() {
  const { session, canManage } = useAuth()
  if (canManage) return <ManagerDashboard />
  if (isFrontDesk(session)) return <FrontDeskDashboard />
  return <ResidentDashboard />
}
