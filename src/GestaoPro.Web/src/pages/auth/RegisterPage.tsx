import { FormEvent, useEffect, useState } from 'react'
import { API_BASE, requestJson } from '../../api/http'
import { SelectOption } from '../../domain/types'
import { isValidCpfFormat, maskCpf, onlyDigits } from '../../lib/masks'

export function RegisterPage({ onBack }: { onBack: () => void }) {
  const [condominiums, setCondominiums] = useState<SelectOption[]>([])
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [registrationNumber, setRegistrationNumber] = useState('')
  const [registeredName, setRegisteredName] = useState('')
  const [form, setForm] = useState({ condominiumId: '', cpf: '', condominiumCode: '', password: '', confirmPassword: '' })

  useEffect(() => {
    fetch(`${API_BASE}/api/Condominiums/public`)
      .then(r => r.ok ? r.json() : Promise.reject())
      .then((data: Record<string, unknown>[]) => {
        setCondominiums(
          data
            .map(c => ({ value: String(c.id ?? ''), label: String(c.name ?? '') }))
            .filter(c => c.value)
        )
      })
      .catch(() => setMessage('Não foi possível carregar a lista de condomínios. Verifique se a API está rodando.'))
  }, [])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!form.condominiumId) { setMessage('Selecione um condomínio.'); return }
    if (!isValidCpfFormat(form.cpf)) { setMessage('Informe um CPF válido no formato 000.000.000-00.'); return }
    if (!form.condominiumCode.trim()) { setMessage('Informe o código do condomínio.'); return }
    if (form.password !== form.confirmPassword) { setMessage('As senhas não coincidem.'); return }
    if (form.password.length < 6) { setMessage('A senha deve ter pelo menos 6 caracteres.'); return }
    setLoading(true)
    setMessage('')
    try {
      const result = await requestJson<{ registrationNumber: string; name: string }>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ cpf: onlyDigits(form.cpf), condominiumId: form.condominiumId, condominiumCode: form.condominiumCode, password: form.password }),
      })
      setRegistrationNumber(result.registrationNumber)
      setRegisteredName(result.name)
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Erro ao cadastrar.')
    } finally {
      setLoading(false)
    }
  }

  if (registrationNumber) {
    return (
      <div className="panel-grid">
        <section className="panel">
          <h2>Cadastro realizado!</h2>
          <p>Olá, <strong>{registeredName}</strong>. Seu acesso foi criado com sucesso.</p>
          <p style={{ margin: '1rem 0 0.25rem' }}>Faça login com o seu e-mail:</p>
          <p style={{ fontSize: '1.2rem', fontWeight: 700, margin: '0 0 1rem' }}>{registrationNumber}</p>
          <p style={{ marginBottom: '1.5rem', color: 'var(--text-muted, #666)' }}>
            Use seu e-mail e a senha que você acabou de definir para entrar.
          </p>
          <button type="button" onClick={onBack}>Ir para Login</button>
        </section>
      </div>
    )
  }

  return (
    <div className="panel-grid">
      <section className="panel">
        <h2>Cadastrar-se</h2>
        <p style={{ marginBottom: '1rem', color: 'var(--text-muted, #666)' }}>
          Seu CPF precisa estar pré-cadastrado pelo administrador do condomínio, que também informa o código do condomínio.
        </p>
        <form onSubmit={handleSubmit} className="form-grid">
          <label>
            Condomínio
            <select
              value={form.condominiumId}
              onChange={e => setForm(f => ({ ...f, condominiumId: e.target.value }))}
              required
            >
              <option value="">Selecione...</option>
              {condominiums.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
            </select>
          </label>
          <label>
            CPF
            <input
              type="text"
              inputMode="numeric"
              maxLength={14}
              placeholder="000.000.000-00"
              value={form.cpf}
              onChange={e => setForm(f => ({ ...f, cpf: maskCpf(e.target.value) }))}
              required
            />
          </label>
          <label>
            Código do condomínio
            <input
              type="password"
              autoComplete="off"
              value={form.condominiumCode}
              onChange={e => setForm(f => ({ ...f, condominiumCode: e.target.value }))}
              required
            />
          </label>
          <label>
            Senha
            <input
              type="password"
              placeholder="Mínimo 6 caracteres"
              value={form.password}
              onChange={e => setForm(f => ({ ...f, password: e.target.value }))}
              required
            />
          </label>
          <label>
            Confirmar senha
            <input
              type="password"
              value={form.confirmPassword}
              onChange={e => setForm(f => ({ ...f, confirmPassword: e.target.value }))}
              required
            />
          </label>
          {message && <div className="notice error">{message}</div>}
          <div className="form-actions">
            <button type="submit" disabled={loading}>{loading ? 'Cadastrando...' : 'Cadastrar'}</button>
            <button type="button" className="secondary-button" onClick={onBack}>Voltar ao Login</button>
          </div>
        </form>
      </section>
    </div>
  )
}
