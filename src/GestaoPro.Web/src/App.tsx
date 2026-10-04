import { useEffect, useState } from 'react'
import './App.css'
import gestaoProLogo from './assets/gestao-pro-logo.svg'
import { SESSION_EXPIRED_EVENT, isRegistrationLike, requestJson, resolveLoggedUserName } from './api/http'
import { isAdminRole, moduleForSession, readSession } from './auth/session'
import { AppSession, LookupData, ModuleDef, ModuleId, SelectOption } from './domain/types'
import { emptyLookups, modules } from './features/modules'
import { PaymentsPage } from './features/payments/PaymentsPage'
import { FilteredPage, UnitsPage, billFilters, occurrenceFilters, petFilters } from './features/resources/FilteredPage'
import { ResourcePage } from './features/resources/ResourcePage'
import { VotesPage } from './features/votes/VotesPage'
import { optionFromRow } from './lib/format'
import { Dashboard } from './pages/Dashboard'
import { AuthPage } from './pages/auth/AuthPage'
import { RegisterPage } from './pages/auth/RegisterPage'

export default function App() {
  const [activeId, setActiveId] = useState<ModuleId>('dashboard')
  const [session, setSession] = useState<AppSession>(() => readSession())
  const [showRegister, setShowRegister] = useState(false)
  const [lookups, setLookups] = useState<LookupData>(emptyLookups)
  const isLoggedIn = Boolean(session.token)
  const canManage = session.accessType === 'admin' && isAdminRole(session.role)
  const canSeeModule = (module: ModuleDef) =>
    (!module.adminOnly || canManage) && !(module.hiddenForRoles ?? []).includes(session.role)
  const visibleModules = modules.filter(module => module.id !== 'auth' && canSeeModule(module))
  const selectedModule = modules.find(module => module.id === activeId) ?? modules[0]
  const fallbackModule = visibleModules[0] ?? modules.find(module => module.id === 'auth')!
  const activeModule = !isLoggedIn ? modules.find(module => module.id === 'auth')! : !canSeeModule(selectedModule) ? fallbackModule : selectedModule
  const pageModule = moduleForSession(activeModule, session, canManage)

  async function refreshLookups(currentSession = session) {
    if (!currentSession.token) {
      setLookups(emptyLookups)
      return
    }

    const readList = async (path: string) => {
      try {
        const data = await requestJson<Record<string, unknown>[]>(path)
        return Array.isArray(data) ? data : []
      } catch {
        return []
      }
    }

    const [condominiums, users, commonAreas, assemblies, units, visitors, deliveries] = await Promise.all([
      readList('/api/Condominiums'),
      readList('/api/Users'),
      readList('/api/CommonAreas'),
      readList('/api/assembly'),
      currentSession.condominiumId ? readList(`/api/Units/condominium/${encodeURIComponent(currentSession.condominiumId)}`) : Promise.resolve([]),
      readList('/api/Visitor'),
      readList('/api/Delivery'),
    ])

    setLookups({
      condominiums: condominiums.map((row, index) => optionFromRow(row, `Condomínio ${index + 1}`)).filter(Boolean) as SelectOption[],
      users: users.map((row, index) => optionFromRow(row, `Usuário ${index + 1}`)).filter(Boolean) as SelectOption[],
      units: units.map((row, index) => optionFromRow(row, `Unidade ${index + 1}`)).filter(Boolean) as SelectOption[],
      commonAreas: commonAreas.map((row, index) => optionFromRow(row, `Área comum ${index + 1}`)).filter(Boolean) as SelectOption[],
      assemblies: assemblies.map((row, index) => optionFromRow(row, `Assembleia ${index + 1}`)).filter(Boolean) as SelectOption[],
      visitors: visitors.map((row, index) => optionFromRow(row, `Visitante ${index + 1}`)).filter(Boolean) as SelectOption[],
      deliveries: deliveries.map((row, index) => optionFromRow(row, `Encomenda ${index + 1}`)).filter(Boolean) as SelectOption[],
    })
  }

  useEffect(() => {
    refreshLookups()
  }, [session.token, session.condominiumId])

  useEffect(() => {
    if (!session.token || !session.userId) return
    if (session.userName && !isRegistrationLike(session.userName)) return

    resolveLoggedUserName(session.userId, session.registrationNumber).then(name => {
      if (!name || name === session.userName || isRegistrationLike(name)) return
      localStorage.setItem('gestaopro_user_name', name)
      setSession(readSession())
    })
  }, [session.token, session.userId, session.userName, session.registrationNumber])

  function updateSession() {
    const nextSession = readSession()
    const nextCanManage = nextSession.accessType === 'admin' && isAdminRole(nextSession.role)
    setSession(nextSession)
    setActiveId(nextCanManage ? 'dashboard' : 'announcements')
    refreshLookups(nextSession)
  }

  function logout() {
    localStorage.removeItem('gestaopro_token')
    localStorage.removeItem('gestaopro_user_id')
    localStorage.removeItem('gestaopro_user_name')
    localStorage.removeItem('gestaopro_role')
    localStorage.removeItem('gestaopro_access_type')
    localStorage.removeItem('gestaopro_condominium_id')
    localStorage.removeItem('gestaopro_registration_number')
    setSession(readSession())
    setLookups(emptyLookups)
    setActiveId('auth')
  }

  // A API devolveu 401 (token expirado): volta para a tela de login em vez de manter o painel aberto.
  useEffect(() => {
    window.addEventListener(SESSION_EXPIRED_EVENT, logout)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, logout)
  }, [])

  if (!isLoggedIn) {
    return (
      <main className="login-shell">
        <div className="login-brand">
          <img className="brand-logo" src={gestaoProLogo} alt="Gestão Pro" />
          <div>
            <strong>Gestão Pro</strong>
            <span>Eficiência para seu condomínio</span>
          </div>
        </div>
        {showRegister
          ? <RegisterPage onBack={() => setShowRegister(false)} />
          : <AuthPage onSessionChange={updateSession} onRegister={() => setShowRegister(true)} />
        }
      </main>
    )
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <img className="brand-logo" src={gestaoProLogo} alt="Gestão Pro" />
          <div>
            <strong>Gestão Pro</strong>
            <span>Condomínio</span>
          </div>
        </div>
        <nav className="nav-list" aria-label="Módulos">
          {visibleModules.map(module => (
            <button
              key={module.id}
              className={activeId === module.id ? 'nav-item active' : 'nav-item'}
              onClick={() => setActiveId(module.id)}
              type="button"
            >
              {module.label}
            </button>
          ))}
        </nav>
      </aside>

      <main className="workspace">
        <header className="page-heading">
          <div>
            <h1>{pageModule.title}</h1>
            <p>{pageModule.description}</p>
          </div>
          <div className="header-actions">
            <span className="hello-user">Olá {session.userName || session.registrationNumber}</span>
            <span className={canManage ? 'access-badge admin' : 'access-badge user'}>
              {canManage ? 'Administrador' : 'Usuário'}
            </span>
            <button type="button" className="secondary-button" onClick={logout}>Sair</button>
          </div>
        </header>

        {activeModule.id === 'dashboard' && <Dashboard />}
        {activeModule.id === 'auth' && <AuthPage onSessionChange={updateSession} />}
        {activeModule.id === 'units' && <UnitsPage module={pageModule} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'pets' && <FilteredPage module={pageModule} filters={petFilters} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'occurrences' && <FilteredPage module={pageModule} filters={occurrenceFilters} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'votes' && <VotesPage module={pageModule} canManage={canManage} session={session} lookups={lookups} />}
        {activeModule.id === 'bills' && <FilteredPage module={pageModule} filters={canManage ? billFilters : null} canManage={canManage} lookups={lookups} />}
        {activeModule.id === 'payments' && <PaymentsPage module={pageModule} canManage={canManage} session={session} lookups={lookups} />}
        {!['dashboard', 'auth', 'units', 'pets', 'occurrences', 'votes', 'bills', 'payments'].includes(activeModule.id) && (
          <ResourcePage
            key={activeModule.id}
            module={pageModule}
            canManage={canManage}
            session={session}
            lookups={lookups}
            onChanged={activeModule.id === 'assembly' ? () => refreshLookups() : undefined}
          />
        )}
      </main>
    </div>
  )
}

// Reexportações usadas pelos testes do painel.
export { formatCurrency } from './lib/format'
export { formInitialValue } from './lib/forms'
export { moduleForSession } from './auth/session'
export { modules } from './features/modules'
