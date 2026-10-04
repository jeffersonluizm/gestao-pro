import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { IoGridOutline, IoLogOutOutline, IoMenuOutline, IoPersonCircleOutline } from 'react-icons/io5'
import { useAuth } from '../auth/AuthContext'
import { roleLabels } from '../auth/session'
import { Brand } from '../components/ui/Brand'
import { LookupsProvider } from '../features/lookups'
import { groupOrder, modulePath, routeOf, visibleModules } from '../features/moduleRoutes'

// Casca do painel: menu lateral por grupos, barra superior com a pessoa logada e o conteúdo da rota.
export function AppShell() {
  const { session, canManage, isManagerRole, signOut, setViewAsResident } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const modules = visibleModules(session, canManage)
  const groups = groupOrder
    .map(group => ({ group, items: modules.filter(module => routeOf(module).group === group) }))
    .filter(entry => entry.items.length)

  useEffect(() => setMenuOpen(false), [location.pathname])

  function logout() {
    signOut()
    navigate('/entrar?motivo=saiu', { replace: true })
  }

  function toggleView() {
    setViewAsResident(canManage)
    navigate('/painel')
  }

  return (
    <LookupsProvider>
      <a className="skip-link" href="#conteudo">Pular para o conteúdo</a>
      <div className="app-shell">
        {menuOpen && <div className="sidebar-backdrop" onClick={() => setMenuOpen(false)} />}
        <aside className="sidebar" data-open={menuOpen} id="menu-lateral">
          <NavLink to="/painel" className="brand" aria-label="Gestão Pro, ir para o painel">
            <Brand tagline={canManage ? 'Gestão do condomínio' : 'Meu condomínio'} />
          </NavLink>
          <nav aria-label="Módulos">
            <div className="sidebar-section">
              <NavLink end to="/painel" className="nav-item"><IoGridOutline aria-hidden size={18} />Painel</NavLink>
            </div>
            {groups.map(({ group, items }) => (
              <div className="sidebar-section" key={group}>
                <p className="sidebar-title">{group}</p>
                {items.map(module => {
                  const Icon = routeOf(module).icon
                  return (
                    <NavLink key={module.id} to={modulePath(module)} className="nav-item">
                      <Icon aria-hidden size={18} />
                      {module.label}
                    </NavLink>
                  )
                })}
              </div>
            ))}
          </nav>
          <p className="sidebar-footer">Gestão Pro · projeto de portfólio</p>
        </aside>

        <div className="workspace">
          <header className="topbar">
            <button
              type="button"
              className="secondary-button menu-toggle"
              aria-controls="menu-lateral"
              aria-expanded={menuOpen}
              onClick={() => setMenuOpen(open => !open)}
            >
              <IoMenuOutline aria-hidden size={20} /> Menu
            </button>
            <div className="topbar-user">
              <span className="hello-user">{session.userName || session.registrationNumber}</span>
              <span className="badge badge-accent">{canManage ? roleLabels[session.role] : isManagerRole ? 'Vendo como morador' : roleLabels[session.role] ?? 'Morador'}</span>
              {session.role === 'sindico' && (
                <button type="button" className="secondary-button" onClick={toggleView}>
                  {canManage ? 'Ver como morador' : 'Voltar à gestão'}
                </button>
              )}
              <NavLink to="/painel/perfil" className="btn btn-secondary"><IoPersonCircleOutline aria-hidden size={18} />Perfil</NavLink>
              <button type="button" className="secondary-button" onClick={logout}><IoLogOutOutline aria-hidden size={18} />Sair</button>
            </div>
          </header>
          <main className="workspace-main" id="conteudo" tabIndex={-1}>
            <Outlet />
          </main>
        </div>
      </div>
    </LookupsProvider>
  )
}
