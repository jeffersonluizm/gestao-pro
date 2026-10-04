import { Link } from 'react-router-dom'

// 403: o módulo existe, mas não é do papel de quem está logado (a API também recusaria).
export function ForbiddenPage() {
  return (
    <section className="system-page">
      <div className="card">
        <p className="system-code" aria-hidden="true">4<span>0</span>3</p>
        <h1>Sem permissão para esta área</h1>
        <p className="muted">Esta parte do sistema é da administração do condomínio. Se precisar de acesso, fale com o síndico.</p>
        <Link className="btn" to="/painel">Voltar ao painel</Link>
      </div>
    </section>
  )
}
