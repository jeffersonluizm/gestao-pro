import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { ReactNode } from 'react'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { ConfirmProvider } from './components/ui/ConfirmDialog'
import { ToastProvider } from './components/ui/Toast'
import { ServerWakeNotice } from './components/ui/ServerWakeNotice'
import { ModulePage } from './features/ModulePage'
import { AppShell } from './layouts/AppShell'
import { AuthLayout } from './layouts/AuthLayout'
import { PublicLayout } from './layouts/PublicLayout'
import { AboutPage } from './pages/AboutPage'
import { DashboardPage } from './pages/DashboardPage'
import { LandingPage } from './pages/LandingPage'
import { ProfilePage } from './pages/ProfilePage'
import { FirstAccessPage } from './pages/auth/FirstAccessPage'
import { LoginPage } from './pages/auth/LoginPage'
import { RecoverPasswordPage } from './pages/auth/RecoverPasswordPage'
import { RegisterPage } from './pages/auth/RegisterPage'
import { ErrorBoundary } from './pages/system/ErrorBoundary'
import { NotFoundPage } from './pages/system/NotFoundPage'

// Painel exige sessão: sem ela, vai para /entrar lembrando a rota de volta.
function RequireSession({ children }: { children: ReactNode }) {
  const { isLoggedIn } = useAuth()
  const location = useLocation()
  if (!isLoggedIn) return <Navigate to={`/entrar?volta=${encodeURIComponent(location.pathname)}`} replace />
  return <>{children}</>
}

// Quem já está logado e abre /entrar ou /cadastro vai direto ao painel.
function GuestOnly({ children }: { children: ReactNode }) {
  const { isLoggedIn } = useAuth()
  return isLoggedIn ? <Navigate to="/painel" replace /> : <>{children}</>
}

export default function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <ToastProvider>
          <ConfirmProvider>
            <ServerWakeNotice />
            <Routes>
              <Route element={<PublicLayout />}>
                <Route path="/" element={<LandingPage />} />
                <Route path="/sobre" element={<AboutPage />} />
              </Route>
              <Route element={<AuthLayout />}>
                <Route path="/entrar" element={<GuestOnly><LoginPage /></GuestOnly>} />
                <Route path="/cadastro" element={<GuestOnly><RegisterPage /></GuestOnly>} />
                <Route path="/primeiro-acesso" element={<FirstAccessPage />} />
                <Route path="/recuperar-senha" element={<RecoverPasswordPage />} />
              </Route>
              <Route path="/painel" element={<RequireSession><AppShell /></RequireSession>}>
                <Route index element={<DashboardPage />} />
                <Route path="perfil" element={<ProfilePage />} />
                <Route path=":slug" element={<ModulePage />} />
                <Route path="*" element={<NotFoundPage embedded />} />
              </Route>
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </ConfirmProvider>
        </ToastProvider>
      </AuthProvider>
    </ErrorBoundary>
  )
}

// Reexportações usadas pelos testes do painel.
export { formatCurrency } from './lib/format'
export { formInitialValue } from './lib/forms'
export { moduleForSession } from './auth/session'
export { modules } from './features/modules'
