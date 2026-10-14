import { useCallback, useEffect, useState } from 'react'
import { requestJson } from '../../api/http'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Announcement } from '../../domain/types'

// Avisos para quem mora e para a portaria: um mural (cartões), mais recentes primeiro.
export function AnnouncementsFeed({ limit }: { limit?: number }) {
  const [items, setItems] = useState<Announcement[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const path = limit ? `/api/announcements/active?page=1&pageSize=${limit}` : '/api/announcements/active'
      const data = await requestJson<Announcement[]>(path)
      setItems(Array.isArray(data) ? data : [])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar os avisos.')
    } finally {
      setLoading(false)
    }
  }, [limit])

  useEffect(() => { load() }, [load])

  if (loading) return <LoadingState label="Carregando avisos" lines={limit ?? 3} />
  if (error) return <ErrorState message={error} onRetry={load} />
  if (!items.length) return <EmptyState title="Nenhum aviso no momento.">Quando a administração publicar, o aviso aparece aqui.</EmptyState>

  return (
    <div className="feed">
      {items.map(item => (
        <article key={item.id} className="card feed-card" aria-labelledby={`aviso-${item.id}`}>
          <div className="feed-meta">
            <span className="badge badge-accent">{item.category || 'Geral'}</span>
            {/* createdAt é um instante (UTC): mostra o dia no fuso de quem lê. */}
            <time dateTime={item.createdAt}>{new Date(item.createdAt).toLocaleDateString('pt-BR')}</time>
          </div>
          <h2 id={`aviso-${item.id}`}>{item.title}</h2>
          <p>{item.content}</p>
        </article>
      ))}
    </div>
  )
}
