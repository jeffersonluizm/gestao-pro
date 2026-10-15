import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { IoBusinessOutline, IoHomeOutline, IoShieldCheckmarkOutline } from 'react-icons/io5'
import { ApiError, requestJson } from '../../api/http'
import { useAuth } from '../../auth/AuthContext'
import { roleLabels } from '../../auth/session'
import { DemoInfo, LoginResponse } from '../../domain/types'

const roleText: Record<string, { label: string; hint: string; icon: typeof IoHomeOutline }> = {
  sindico: { label: 'síndico', hint: 'Gestão: cadastros, reservas, avisos e financeiro', icon: IoShieldCheckmarkOutline },
  morador: { label: 'morador', hint: 'Avisos, reservas, encomendas e cobranças', icon: IoHomeOutline },
  porteiro: { label: 'porteiro', hint: 'Visitantes e encomendas na portaria', icon: IoBusinessOutline },
}

// Papel da última entrada na demo e hora da última nova tentativa automática (sessionStorage).
export const DEMO_ROLE_KEY = 'gestaopro_demo_role'
export const DEMO_RETRY_KEY = 'gestaopro_demo_retry'
const RETRY_DELAY_MS = 1500
// Uma nova tentativa automática por minuto: se a sessão nova também cair, a pessoa decide (sem laço).
const AUTO_RETRY_WINDOW_MS = 60_000

function readSession(key: string) {
  try { return sessionStorage.getItem(key) } catch { return null }
}

function writeSession(key: string, value: string | null) {
  try {
    if (value === null) sessionStorage.removeItem(key)
    else sessionStorage.setItem(key, value)
  } catch { /* sem sessionStorage: segue sem lembrar */ }
}

const wait = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

// Cartão "Explore a demonstração": só aparece quando a API está em modo demo (GET /api/demo responde).
// A entrada é pelo papel (POST /api/demo/entrar): nenhuma senha passa pelo painel nem aparece na tela.
// Se a sessão demo cair (401 depois de um reset dos dados), o cartão recarrega e entra de novo uma vez.
export function DemoAccess({ compact = false }: { compact?: boolean }) {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [demo, setDemo] = useState<DemoInfo | null>(null)
  const [busyRole, setBusyRole] = useState('')
  const [error, setError] = useState('')
  const autoRetried = useRef(false)

  const enterAs = useCallback(async (role: string, automatic = false) => {
    setBusyRole(role)
    setError('')
    if (!automatic) writeSession(DEMO_RETRY_KEY, null)
    const enter = () => requestJson<LoginResponse>('/api/demo/entrar', { method: 'POST', body: JSON.stringify({ papel: role }) })
    try {
      let response: LoginResponse
      try {
        response = await enter()
      } catch (err) {
        // Demonstração sendo preparada (o servidor acabou de acordar ou de restaurar os dados): tenta mais uma vez.
        if (!(err instanceof ApiError) || err.status !== 503) throw err
        await wait(RETRY_DELAY_MS)
        response = await enter()
      }
      writeSession(DEMO_ROLE_KEY, role)
      signIn(response)
      navigate('/painel', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível entrar na demonstração.')
    } finally {
      setBusyRole('')
    }
  }, [navigate, signIn])

  useEffect(() => {
    let active = true
    requestJson<DemoInfo>('/api/demo')
      .then(info => { if (active && info?.accounts?.length) setDemo(info) })
      .catch(() => undefined) // sem modo demo (404) ou API fora: o cartão simplesmente não aparece
    return () => { active = false }
  }, [])

  // Sessão demo encerrada (401 depois do reset): entra de novo no mesmo papel, uma vez só.
  useEffect(() => {
    if (!demo || autoRetried.current || params.get('motivo') !== 'sessao') return
    const role = readSession(DEMO_ROLE_KEY)
    const lastRetry = Number(readSession(DEMO_RETRY_KEY) ?? 0)
    if (!role || Date.now() - lastRetry < AUTO_RETRY_WINDOW_MS || !demo.accounts.some(account => account.role === role)) return
    autoRetried.current = true
    writeSession(DEMO_RETRY_KEY, String(Date.now()))
    void enterAs(role, true)
  }, [demo, enterAs, params])

  if (!demo) return null

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
        <summary>Testar a ativação de acesso (código do condomínio e CPFs)</summary>
        <dl>
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
