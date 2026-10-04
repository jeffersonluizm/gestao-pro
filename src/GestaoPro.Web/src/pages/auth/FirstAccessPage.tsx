import { FormEvent, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { requestJson } from '../../api/http'
import { useAuth } from '../../auth/AuthContext'
import { PasswordInput } from '../../components/ui/PasswordInput'
import { LoginResponse } from '../../domain/types'
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../lib/passwordPolicy'

interface FirstAccessState {
  userId: string
  login: string
  currentPassword: string
}

// Primeiro acesso (conta criada pela administração ou senha redefinida): troca obrigatória e entrada direta.
export function FirstAccessPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const state = useLocation().state as FirstAccessState | null
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  if (!state?.userId) {
    return (
      <div className="auth-card">
        <header>
          <h1>Primeiro acesso</h1>
          <p>Para criar sua senha, entre primeiro com o e-mail e a senha temporária que a administração entregou.</p>
        </header>
        <Link className="btn" to="/entrar">Ir para Entrar</Link>
      </div>
    )
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (!isStrongPassword(newPassword)) return setError(WEAK_PASSWORD_MESSAGE)
    if (newPassword !== confirmPassword) return setError('A confirmação não confere com a nova senha.')
    if (newPassword === state!.currentPassword) return setError('A nova senha precisa ser diferente da atual.')
    setBusy(true)
    try {
      await requestJson('/api/Auth/change-password', {
        method: 'PATCH',
        body: JSON.stringify({ userId: state!.userId, currentPassword: state!.currentPassword, newPassword }),
      })
      const response = await requestJson<LoginResponse>('/api/Auth/login', {
        method: 'POST',
        body: JSON.stringify({ registrationNumber: state!.login, password: newPassword }),
      })
      signIn(response)
      navigate('/painel', { replace: true, state: null })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível trocar a senha.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-card">
      <header>
        <p className="eyebrow">Segurança</p>
        <h1>Primeiro acesso</h1>
        <p>Crie a sua senha pessoal. Ela substitui a senha temporária usada para entrar agora.</p>
      </header>
      <form className="form-stack" onSubmit={submit} noValidate>
        <div className="field">
          <label className="field-label" htmlFor="nova-senha">Nova senha</label>
          <PasswordInput id="nova-senha" autoComplete="new-password" aria-describedby="regra-senha" value={newPassword} onChange={event => setNewPassword(event.target.value)} />
          <span className="field-hint" id="regra-senha">De 8 a 128 caracteres, sem sequências como 12345678.</span>
        </div>
        <div className="field">
          <label className="field-label" htmlFor="confirmar-senha">Confirmar nova senha</label>
          <PasswordInput id="confirmar-senha" autoComplete="new-password" value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} />
        </div>
        {error && <div className="alert alert-error" role="alert">{error}</div>}
        <button type="submit" className="btn-large" disabled={busy}>{busy ? 'Salvando...' : 'Salvar e entrar'}</button>
      </form>
    </div>
  )
}
