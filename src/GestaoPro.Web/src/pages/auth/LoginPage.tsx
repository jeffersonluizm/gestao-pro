import { FormEvent, useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { requestJson } from '../../api/http'
import { useAuth } from '../../auth/AuthContext'
import { PasswordInput } from '../../components/ui/PasswordInput'
import { LoginResponse, MustChangePasswordResponse } from '../../domain/types'

const avisos: Record<string, string> = {
  sessao: 'Sua sessão expirou. Entre novamente para continuar de onde parou.',
  saiu: 'Você saiu do Gestão Pro.',
  senha: 'Senha alterada. Entre com a senha nova.',
}

// Só aceita rota interna do painel como destino depois do login (evita redirecionamento aberto).
export function safeReturnPath(value: string | null) {
  return value && value.startsWith('/painel') && !value.startsWith('//') ? value : '/painel'
}

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [params] = useSearchParams()
  const [login, setLogin] = useState(String((location.state as { login?: string } | null)?.login ?? ''))
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const aviso = avisos[params.get('motivo') ?? '']

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (!login.trim() || !password) {
      setError('Informe o e-mail e a senha.')
      return
    }
    setBusy(true)
    try {
      const response = await requestJson<LoginResponse | MustChangePasswordResponse>('/api/Auth/login', {
        method: 'POST',
        body: JSON.stringify({ registrationNumber: login.trim(), password }),
      })
      if (response.mustChangePassword && !('token' in response && response.token)) {
        // A senha atual fica só na memória da navegação, para a troca do primeiro acesso.
        navigate('/primeiro-acesso', { state: { userId: response.userId, login: login.trim(), currentPassword: password } })
        return
      }
      signIn(response as LoginResponse)
      navigate(safeReturnPath(params.get('volta')), { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível entrar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-card">
      <header>
        <p className="eyebrow">Acesso</p>
        <h1>Entrar</h1>
        <p>Use o e-mail cadastrado no condomínio e a sua senha.</p>
      </header>
      {aviso && <div className="alert alert-info" role="status">{aviso}</div>}
      <form className="form-stack" onSubmit={submit} noValidate>
        <div className="field">
          <label className="field-label" htmlFor="login-email">E-mail</label>
          <input id="login-email" type="email" autoComplete="username" value={login} onChange={event => setLogin(event.target.value)} required />
        </div>
        <div className="field">
          <label className="field-label" htmlFor="login-senha">Senha</label>
          <PasswordInput id="login-senha" autoComplete="current-password" value={password} onChange={event => setPassword(event.target.value)} required />
        </div>
        {error && <div className="alert alert-error" role="alert">{error}</div>}
        <button type="submit" className="btn-large" disabled={busy}>{busy ? 'Entrando...' : 'Entrar'}</button>
      </form>
      <div className="auth-links">
        <Link to="/recuperar-senha">Esqueci minha senha</Link>
        <span>Primeira vez aqui? <Link to="/cadastro">Ative o seu acesso de morador</Link></span>
      </div>
      {import.meta.env.DEV && (
        <div className="demo-box">
          <strong>Ambiente de desenvolvimento.</strong> A API cria a conta <code>admin@gestaopro.local</code> (senha <code>Admin@123</code>) e o condomínio de demonstração com o código <code>Demo-Condominio-2026</code>.
        </div>
      )}
    </div>
  )
}
