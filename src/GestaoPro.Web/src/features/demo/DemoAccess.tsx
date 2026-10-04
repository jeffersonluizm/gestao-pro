import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { IoBusinessOutline, IoHomeOutline, IoShieldCheckmarkOutline } from 'react-icons/io5'
import { requestJson } from '../../api/http'
import { useAuth } from '../../auth/AuthContext'
import { roleLabels } from '../../auth/session'
import { DemoInfo, LoginResponse } from '../../domain/types'

const roleText: Record<string, { label: string; hint: string; icon: typeof IoHomeOutline }> = {
  sindico: { label: 'síndico', hint: 'Gestão: cadastros, reservas, avisos e financeiro', icon: IoShieldCheckmarkOutline },
  morador: { label: 'morador', hint: 'Avisos, reservas, encomendas e cobranças', icon: IoHomeOutline },
  porteiro: { label: 'porteiro', hint: 'Visitantes e encomendas na portaria', icon: IoBusinessOutline },
}

// Cartão "Explore a demonstração": só aparece quando a API está em modo demo (GET /api/demo responde).
// As credenciais nunca ficam no painel: vêm da API, que as lê da configuração do servidor.
export function DemoAccess({ compact = false }: { compact?: boolean }) {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const [demo, setDemo] = useState<DemoInfo | null>(null)
  const [busyRole, setBusyRole] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    requestJson<DemoInfo>('/api/demo')
      .then(info => { if (active && info?.accounts?.length) setDemo(info) })
      .catch(() => undefined) // sem modo demo (404) ou API fora: o cartão simplesmente não aparece
    return () => { active = false }
  }, [])

  if (!demo) return null

  async function enterAs(role: string) {
    const account = demo!.accounts.find(item => item.role === role)
    if (!account) return
    setBusyRole(role)
    setError('')
    try {
      const response = await requestJson<LoginResponse>('/api/Auth/login', {
        method: 'POST',
        body: JSON.stringify({ registrationNumber: account.email, password: account.password }),
      })
      signIn(response)
      navigate('/painel', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível entrar na demonstração.')
    } finally {
      setBusyRole('')
    }
  }

  return (
    <section className={compact ? 'demo-card compact' : 'demo-card'} aria-labelledby="demo-titulo">
      <div className="demo-card-header">
        <p className="eyebrow">Demonstração</p>
        <h2 id="demo-titulo">Explore a demonstração</h2>
        <p>Entre com um clique no {demo.condominiumName}. Os dados voltam ao estado inicial de tempos em tempos.</p>
      </div>
      <div className="demo-roles">
        {demo.accounts.map(account => {
          const info = roleText[account.role] ?? { label: roleLabels[account.role] ?? account.role, hint: '', icon: IoHomeOutline }
          const Icon = info.icon
          return (
            <button
              key={account.role}
              type="button"
              className="demo-role"
              onClick={() => enterAs(account.role)}
              disabled={Boolean(busyRole)}
              aria-describedby={`demo-${account.role}`}
            >
              <Icon aria-hidden size={20} />
              <span>
                <strong>{busyRole === account.role ? 'Entrando...' : `Entrar como ${info.label}`}</strong>
                <span id={`demo-${account.role}`} className="demo-role-hint">{account.name} · {info.hint}</span>
              </span>
            </button>
          )
        })}
      </div>
      <details className="demo-details">
        <summary>Ver e-mails, senha e código do condomínio</summary>
        <dl>
          {demo.accounts.map(account => (
            <div key={account.email}><dt>{roleLabels[account.role] ?? account.role}</dt><dd><code>{account.email}</code></dd></div>
          ))}
          <div><dt>Senha das contas</dt><dd><code>{demo.accounts[0].password}</code></dd></div>
          <div><dt>Código do condomínio (cadastro)</dt><dd><code>{demo.condominiumCode}</code></dd></div>
          {demo.preRegistered?.length ? (
            <div><dt>CPFs pré-cadastrados para ativar acesso</dt><dd>{demo.preRegistered.map(item => <code key={item.cpf} style={{ marginLeft: 4 }}>{item.cpf}</code>)}</dd></div>
          ) : null}
        </dl>
      </details>
      {error && <div className="alert alert-error" role="alert">{error}</div>}
    </section>
  )
}
