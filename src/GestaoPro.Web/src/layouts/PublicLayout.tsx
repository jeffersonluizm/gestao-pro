import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { Brand } from '../components/ui/Brand'

// Páginas públicas (início e sobre): cabeçalho com navegação e rodapé.
export function PublicLayout() {
  const { isLoggedIn } = useAuth()
  return (
    <>
      <a className="skip-link" href="#conteudo">Pular para o conteúdo</a>
      <header className="public-header">
        <Link to="/" className="brand" aria-label="Gestão Pro, página inicial"><Brand /></Link>
        <nav className="public-nav" aria-label="Principal">
          <Link to="/sobre">Como foi feito</Link>
          {isLoggedIn
            ? <Link className="btn" to="/painel">Ir para o painel</Link>
            : <Link className="btn" to="/entrar">Entrar</Link>}
        </nav>
      </header>
      <main id="conteudo">
        <Outlet />
      </main>
      <footer className="public-footer">
        <p>Gestão Pro · sistema de gestão condominial feito como projeto acadêmico e de portfólio (API .NET, painel React e app Expo).</p>
        <p>Dados de demonstração. <Link to="/sobre">Limitações e decisões técnicas</Link>.</p>
      </footer>
    </>
  )
}
