import { Link } from 'react-router-dom'

// 404. Dentro do painel (embedded) volta ao painel; fora dele, ao início.
export function NotFoundPage({ embedded = false }: { embedded?: boolean }) {
  return (
    <main className="system-page" id="conteudo">
      <div className="card">
        <p className="system-code" aria-hidden="true">4<span>0</span>4</p>
        <h1>Página não encontrada</h1>
        <p className="muted">O endereço pode ter mudado ou ter sido digitado errado.</p>
        <Link className="btn" to={embedded ? '/painel' : '/'}>{embedded ? 'Voltar ao painel' : 'Voltar ao início'}</Link>
      </div>
    </main>
  )
}
