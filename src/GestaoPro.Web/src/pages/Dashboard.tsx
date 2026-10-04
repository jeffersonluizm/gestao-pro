import { useEffect, useState } from 'react'
import { requestJson } from '../api/http'

export function Dashboard() {
  const probes = [
    { label: 'Usuários', path: '/api/Users' },
    { label: 'Áreas comuns', path: '/api/CommonAreas' },
    { label: 'Reservas', path: '/api/reservations' },
    { label: 'Avisos', path: '/api/announcements' },
    { label: 'Visitantes', path: '/api/Visitor' },
    { label: 'Pacotes', path: '/api/Package' },
    { label: 'Encomendas', path: '/api/Delivery' },
    { label: 'Assembleias', path: '/api/assembly' },
  ]
  const [counts, setCounts] = useState<Record<string, number | string>>({})

  useEffect(() => {
    Promise.all(
      probes.map(async item => {
        try {
          const data = await requestJson<unknown[]>(item.path)
          return [item.label, Array.isArray(data) ? data.length : 0] as const
        } catch {
          return [item.label, 'sem conexão'] as const
        }
      })
    ).then(results => setCounts(Object.fromEntries(results)))
  }, [])

  return (
    <section className="dashboard-grid">
      {probes.map(item => (
        <article className="metric-card" key={item.label}>
          <span>{item.label}</span>
          <strong>{counts[item.label] ?? '-'}</strong>
        </article>
      ))}
    </section>
  )
}
