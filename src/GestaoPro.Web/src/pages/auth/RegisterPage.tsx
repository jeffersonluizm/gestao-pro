import { FormEvent, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { requestJson } from '../../api/http'
import { PasswordInput } from '../../components/ui/PasswordInput'
import { CheckCpfResponse, CondominiumSummary, SelfRegisterResponse } from '../../domain/types'
import { isValidCpfFormat, maskCpf, maskPhoneBR, onlyDigits } from '../../lib/masks'
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../lib/passwordPolicy'

type Step = 'vinculo' | 'dados' | 'pronto'

// Ativação do acesso de morador em dois passos:
// 1) condomínio + CPF + código do condomínio (a API confere sem dizer o motivo de uma recusa);
// 2) dados que faltam no pré-cadastro (nome e e-mail de login) e a senha.
export function RegisterPage() {
  const [step, setStep] = useState<Step>('vinculo')
  const [condominiums, setCondominiums] = useState<CondominiumSummary[]>([])
  const [needsProfileData, setNeedsProfileData] = useState(false)
  const [form, setForm] = useState({ condominiumId: '', cpf: '', condominiumCode: '', name: '', email: '', phone: '', password: '', confirmPassword: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<SelfRegisterResponse | null>(null)

  useEffect(() => {
    requestJson<CondominiumSummary[]>('/api/Condominiums/public')
      .then(data => setCondominiums(Array.isArray(data) ? data : []))
      .catch(() => setError('Não foi possível carregar a lista de condomínios. Tente de novo em instantes.'))
  }, [])

  const set = (key: keyof typeof form) => (value: string) => setForm(current => ({ ...current, [key]: value }))

  async function checkLink(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (!form.condominiumId) return setError('Selecione o condomínio.')
    if (!isValidCpfFormat(form.cpf)) return setError('Informe um CPF válido no formato 000.000.000-00.')
    if (!form.condominiumCode.trim()) return setError('Informe o código do condomínio que a administração entregou.')
    setBusy(true)
    try {
      const response = await requestJson<CheckCpfResponse>('/api/auth/check-cpf', {
        method: 'POST',
        body: JSON.stringify({ cpf: onlyDigits(form.cpf), condominiumId: form.condominiumId, condominiumCode: form.condominiumCode }),
      })
      if (!response.canProceed) return setError(response.message)
      setNeedsProfileData(response.needsProfileData)
      setStep('dados')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível conferir os dados.')
    } finally {
      setBusy(false)
    }
  }

  async function register(event: FormEvent) {
    event.preventDefault()
    setError('')
    if (needsProfileData && !form.name.trim()) return setError('Informe o seu nome completo.')
    if (needsProfileData && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) return setError('Informe um e-mail válido: ele será o seu login.')
    if (form.phone && onlyDigits(form.phone).length !== 10) return setError('Informe o telefone no formato 31 9999-2702 ou deixe em branco.')
    if (!isStrongPassword(form.password)) return setError(WEAK_PASSWORD_MESSAGE)
    if (form.password !== form.confirmPassword) return setError('A confirmação não confere com a senha.')
    setBusy(true)
    try {
      const response = await requestJson<SelfRegisterResponse>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({
          cpf: onlyDigits(form.cpf),
          condominiumId: form.condominiumId,
          condominiumCode: form.condominiumCode,
          password: form.password,
          name: needsProfileData ? form.name.trim() : undefined,
          email: needsProfileData ? form.email.trim() : undefined,
          phone: form.phone ? onlyDigits(form.phone) : undefined,
        }),
      })
      setResult(response)
      setStep('pronto')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível criar o acesso.')
    } finally {
      setBusy(false)
    }
  }

  const steps = (
    <ol className="steps" aria-label="Etapas do cadastro">
      <li className={step !== 'vinculo' ? 'done' : ''} aria-current={step === 'vinculo' ? 'step' : undefined}>1. Vínculo</li>
      <li className={step === 'pronto' ? 'done' : ''} aria-current={step === 'dados' ? 'step' : undefined}>2. Dados e senha</li>
      <li aria-current={step === 'pronto' ? 'step' : undefined}>3. Pronto</li>
    </ol>
  )

  if (step === 'pronto' && result) {
    return (
      <div className="auth-card">
        {steps}
        <header>
          <h1>Acesso criado</h1>
          <p>Olá, {result.name}. A partir de agora, entre com:</p>
        </header>
        <p className="card" style={{ fontWeight: 700, fontSize: '1.15rem' }}>{result.registrationNumber}</p>
        <p className="muted">e a senha que você acabou de definir.</p>
        <Link className="btn btn-large" to="/entrar" state={{ login: result.registrationNumber }}>Ir para Entrar</Link>
      </div>
    )
  }

  return (
    <div className="auth-card">
      {steps}
      <header>
        <p className="eyebrow">Morador</p>
        <h1>Ative o seu acesso</h1>
        <p>{step === 'vinculo'
          ? 'Seu CPF precisa ter sido pré-cadastrado pela administração, que também entrega o código do condomínio.'
          : needsProfileData ? 'Complete os seus dados. O e-mail será o seu login.' : 'Defina a sua senha.'}</p>
      </header>

      {step === 'vinculo' ? (
        <form className="form-stack" onSubmit={checkLink} noValidate>
          <div className="field">
            <label className="field-label" htmlFor="cad-condominio">Condomínio</label>
            <select id="cad-condominio" value={form.condominiumId} onChange={event => set('condominiumId')(event.target.value)}>
              <option value="">Selecione</option>
              {condominiums.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </div>
          <div className="field">
            <label className="field-label" htmlFor="cad-cpf">CPF</label>
            <input id="cad-cpf" inputMode="numeric" maxLength={14} placeholder="000.000.000-00" value={form.cpf} onChange={event => set('cpf')(maskCpf(event.target.value))} />
          </div>
          <div className="field">
            <label className="field-label" htmlFor="cad-codigo">Código do condomínio</label>
            <PasswordInput id="cad-codigo" autoComplete="off" value={form.condominiumCode} onChange={event => set('condominiumCode')(event.target.value)} />
          </div>
          {error && <div className="alert alert-error" role="alert">{error}</div>}
          <button type="submit" className="btn-large" disabled={busy}>{busy ? 'Conferindo...' : 'Continuar'}</button>
        </form>
      ) : (
        <form className="form-stack" onSubmit={register} noValidate>
          {needsProfileData && (
            <>
              <div className="field">
                <label className="field-label" htmlFor="cad-nome">Nome completo</label>
                <input id="cad-nome" autoComplete="name" value={form.name} onChange={event => set('name')(event.target.value)} />
              </div>
              <div className="field">
                <label className="field-label" htmlFor="cad-email">E-mail</label>
                <input id="cad-email" type="email" autoComplete="email" value={form.email} onChange={event => set('email')(event.target.value)} />
              </div>
              <div className="field">
                <label className="field-label" htmlFor="cad-telefone">Telefone (opcional)</label>
                <input id="cad-telefone" inputMode="numeric" maxLength={12} placeholder="31 9999-2702" value={form.phone} onChange={event => set('phone')(maskPhoneBR(event.target.value))} />
              </div>
            </>
          )}
          <div className="field">
            <label className="field-label" htmlFor="cad-senha">Senha</label>
            <PasswordInput id="cad-senha" autoComplete="new-password" aria-describedby="cad-regra" value={form.password} onChange={event => set('password')(event.target.value)} />
            <span className="field-hint" id="cad-regra">De 8 a 128 caracteres, sem sequências como 12345678.</span>
          </div>
          <div className="field">
            <label className="field-label" htmlFor="cad-confirmar">Confirmar senha</label>
            <PasswordInput id="cad-confirmar" autoComplete="new-password" value={form.confirmPassword} onChange={event => set('confirmPassword')(event.target.value)} />
          </div>
          {error && <div className="alert alert-error" role="alert">{error}</div>}
          <div className="form-actions">
            <button type="button" className="secondary-button" onClick={() => { setStep('vinculo'); setError('') }}>Voltar</button>
            <button type="submit" disabled={busy}>{busy ? 'Criando...' : 'Criar acesso'}</button>
          </div>
        </form>
      )}

      <div className="auth-links">
        <span>Já tem acesso? <Link to="/entrar">Entrar</Link></span>
      </div>
    </div>
  )
}
