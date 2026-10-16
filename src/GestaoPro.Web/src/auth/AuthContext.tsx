import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { SESSION_EXPIRED_EVENT } from '../api/http'
import { AppSession, LoginResponse } from '../domain/types'
import { canManageSession, clearStoredSession, isAdminRole, readSession, storeAccessType, storeLogin, storeUserName } from './session'

interface AuthContextValue {
  session: AppSession
  isLoggedIn: boolean
  // Visão de gestão ativa (papel admin/síndico e sem "ver como morador").
  canManage: boolean
  // Papel de gestão, independente da visão escolhida.
  isManagerRole: boolean
  signIn: (response: LoginResponse) => AppSession
  signOut: () => void
  setViewAsResident: (asResident: boolean) => void
  setUserName: (name: string) => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AppSession>(() => readSession())
  const navigate = useNavigate()
  const location = useLocation()

  const signIn = useCallback((response: LoginResponse) => {
    const next = storeLogin(response)
    setSession(next)
    return next
  }, [])

  const signOut = useCallback(() => {
    clearStoredSession()
    setSession(readSession())
  }, [])

  const setViewAsResident = useCallback((asResident: boolean) => {
    storeAccessType(asResident ? 'usuario' : 'admin')
    setSession(readSession())
  }, [])

  const setUserName = useCallback((name: string) => {
    storeUserName(name)
    setSession(readSession())
  }, [])

  // A API respondeu 401 (token vencido ou revogado): volta ao login com aviso e lembra onde a pessoa estava.
  useEffect(() => {
    function onExpired() {
      const volta = location.pathname.startsWith('/painel') ? `&volta=${encodeURIComponent(location.pathname)}` : ''
      navigate(`/entrar?motivo=sessao${volta}`, { replace: true })
      setSession(readSession())
    }
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired)
  }, [location.pathname, navigate])

  const value = useMemo<AuthContextValue>(() => ({
    session,
    isLoggedIn: Boolean(session.token),
    canManage: canManageSession(session),
    isManagerRole: isAdminRole(session.role),
    signIn,
    signOut,
    setViewAsResident,
    setUserName,
  }), [session, signIn, signOut, setViewAsResident, setUserName])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth precisa estar dentro de AuthProvider.')
  return context
}
