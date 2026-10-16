import { FormEvent, useCallback, useEffect, useState } from 'react'
import { requestJson } from '../api/http'
import { useAuth } from '../auth/AuthContext'
import { roleLabels } from '../auth/session'
import { PasswordInput } from '../components/ui/PasswordInput'
import { ErrorState, LoadingState } from '../components/ui/States'
import { UserProfile } from '../domain/types'
import { maskPhoneBR } from '../lib/masks'
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../lib/passwordPolicy'

// Perfil: dados da própria conta e troca de senha (exige a senha atual).
export function ProfilePage() {
  const { session } = useAuth()
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [loadError, setLoadError] = useState('')
  const [form, setForm] = useState({ current: '', next: '', confirm: '' })
  const [error, setError] = useState('')
  const [done, setDone] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setLoadError('')
    try {
      setProfile(await requestJson<UserProfile>(`/api/Users/${encodeURIComponent(session.userId)}`))
    } catch (err) {
      setLoadError(err instanceof Error ? err.message : 'Não foi possível carregar o perfil.')
    }
  }, [session.userId])

  useEffect(() => { load() }, [load])

  async function changePassword(event: FormEvent) {
    event.preventDefault()
    setError('')
    setDone('')
    if (!form.current) return setError('Informe a senha atual.')
    if (!isStrongPassword(form.next)) return setError(WEAK_PASSWORD_MESSAGE)
    if (form.next !== form.confirm) return setError('A confirmação não confere com a nova senha.')
    setBusy(true)
    try {
      await requestJson(`/api/Users/${encodeURIComponent(session.userId)}/change-password`, {
        method: 'PATCH',
        body: JSON.stringify({ currentPassword: form.current, newPassword: form.next }),
      })
      setForm({ current: '', next: '', confirm: '' })
      setDone('Senha alterada.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível trocar a senha.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <header className="page-heading">
        <div>
          <h1>Meu perfil</h1>
          <p>Seus dados de acesso e a troca de senha.</p>
        </div>
      </header>
      <div className="panel-grid two">
        <section className="panel" aria-labelledby="perfil-dados">
          <h2 id="perfil-dados">Dados da conta</h2>
          {loadError ? <ErrorState message={loadError} onRetry={load} /> : !profile ? <LoadingState lines={4} /> : (
            <dl className="list-plain" style={{ margin: 0 }}>
              <div><dt className="muted">Nome</dt><dd style={{ margin: 0, fontWeight: 600 }}>{profile.name}</dd></div>
              <div><dt className="muted">E-mail de login</dt><dd style={{ margin: 0 }}>{profile.email || session.registrationNumber}</dd></div>
              <div><dt className="muted">Telefone</dt><dd style={{ margin: 0 }}>{profile.phone ? maskPhoneBR(profile.phone) : 'Não informado'}</dd></div>
              <div><dt className="muted">Perfil</dt><dd style={{ margin: 0 }}>{roleLabels[profile.role] ?? profile.role}</dd></div>
            </dl>
          )}
          <p className="muted" style={{ marginTop: 16 }}>Para mudar nome, e-mail ou telefone, fale com a administração do condomínio.</p>
        </section>
        <section className="panel" aria-labelledby="perfil-senha">
          <h2 id="perfil-senha">Trocar senha</h2>
          <form className="form-stack" onSubmit={changePassword} noValidate>
            <div className="field">
              <label className="field-label" htmlFor="senha-atual">Senha atual</label>
              <PasswordInput id="senha-atual" autoComplete="current-password" value={form.current} onChange={event => setForm(f => ({ ...f, current: event.target.value }))} />
            </div>
            <div className="field">
              <label className="field-label" htmlFor="senha-nova">Nova senha</label>
              <PasswordInput id="senha-nova" autoComplete="new-password" aria-describedby="perfil-regra" value={form.next} onChange={event => setForm(f => ({ ...f, next: event.target.value }))} />
              <span className="field-hint" id="perfil-regra">De 8 a 128 caracteres, sem sequências como 12345678.</span>
            </div>
            <div className="field">
              <label className="field-label" htmlFor="senha-confirmar">Confirmar nova senha</label>
              <PasswordInput id="senha-confirmar" autoComplete="new-password" value={form.confirm} onChange={event => setForm(f => ({ ...f, confirm: event.target.value }))} />
            </div>
            {error && <div className="alert alert-error" role="alert">{error}</div>}
            {done && <div className="alert alert-success" role="status">{done}</div>}
            <div className="form-actions">
              <button type="submit" disabled={busy}>{busy ? 'Salvando...' : 'Trocar senha'}</button>
            </div>
          </form>
        </section>
      </div>
    </>
  )
}
