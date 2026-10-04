import { useState } from 'react'
import { requestJson, resolveLoggedUserName } from '../../api/http'
import { isAdminRole } from '../../auth/session'
import { JsonForm } from '../../components/form/ResourceForm'
import { FieldDef } from '../../domain/types'
import { modules } from '../../features/modules'
import { formatOptionLabel } from '../../lib/labels'

export function AuthPage({ onSessionChange, onRegister }: { onSessionChange: () => void; onRegister?: () => void }) {
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [pendingPasswordUserId, setPendingPasswordUserId] = useState('')
  const changePasswordFields: FieldDef[] = [
    { name: 'currentPassword', label: 'Senha atual', type: 'password', required: true },
    { name: 'newPassword', label: 'Nova senha', type: 'password', required: true },
  ]

  async function handleLogin(payload: Record<string, unknown>) {
    setLoading(true)
    setMessage('')
    try {
      const requestedAccess = String(payload.accessType || 'usuario')
      const credentials = { ...payload }
      delete credentials.accessType
      const response = await requestJson<Record<string, unknown>>('/api/Auth/login', {
        method: 'POST',
        body: JSON.stringify(credentials),
      })
      if (response.mustChangePassword && response.userId) {
        setPendingPasswordUserId(String(response.userId))
        setMessage('Primeiro acesso: troque a senha para continuar.')
        return
      }
      const responseRole = String(response.role ?? '')
      if (requestedAccess === 'admin' && !isAdminRole(responseRole)) {
        localStorage.removeItem('gestaopro_token')
        localStorage.removeItem('gestaopro_user_id')
        localStorage.removeItem('gestaopro_user_name')
        localStorage.removeItem('gestaopro_role')
        localStorage.removeItem('gestaopro_access_type')
        localStorage.removeItem('gestaopro_condominium_id')
        localStorage.removeItem('gestaopro_registration_number')
        setMessage('Essa matrícula não possui acesso de administrador.')
        return
      }
      if (response.token) localStorage.setItem('gestaopro_token', String(response.token))
      if (response.userId) localStorage.setItem('gestaopro_user_id', String(response.userId))
      if (response.role) localStorage.setItem('gestaopro_role', String(response.role))
      localStorage.setItem('gestaopro_access_type', requestedAccess)
      if (response.condominiumId) localStorage.setItem('gestaopro_condominium_id', String(response.condominiumId))
      if (response.registrationNumber) localStorage.setItem('gestaopro_registration_number', String(response.registrationNumber))
      const fallbackName = String(response.userName || response.registrationNumber || '')
      const userName = await resolveLoggedUserName(String(response.userId || ''), fallbackName)
      if (userName) localStorage.setItem('gestaopro_user_name', userName)
      onSessionChange()
      setPendingPasswordUserId('')
      setMessage(`Login realizado como ${formatOptionLabel(requestedAccess)}.`)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao autenticar.')
    } finally {
      setLoading(false)
    }
  }

  async function handleChangePassword(payload: Record<string, unknown>) {
    if (!pendingPasswordUserId) return
    await requestJson('/api/Auth/change-password', {
      method: 'PATCH',
      body: JSON.stringify({ userId: pendingPasswordUserId, ...payload }),
    })
    setPendingPasswordUserId('')
    setMessage('Senha alterada. Faça login novamente com a nova senha.')
  }

  return (
    <div className="panel-grid two">
      <JsonForm title="Login" fields={modules.find(m => m.id === 'auth')?.fields ?? []} onSubmit={handleLogin} submitting={loading} submitLabel="Entrar" successMessage="" />
      {pendingPasswordUserId && <JsonForm title="Trocar senha" fields={changePasswordFields} onSubmit={handleChangePassword} />}
      {message && <div className="notice error span-two">{message}</div>}
      {onRegister && !pendingPasswordUserId && (
        <div className="notice span-two" style={{ textAlign: 'center' }}>
          Primeira vez no sistema?{' '}
          <button type="button" className="link-button" onClick={onRegister}>Cadastrar-se</button>
        </div>
      )}
    </div>
  )
}
