import { FormEvent, useEffect, useMemo, useState } from 'react'
import { FieldDef } from '../../domain/types'
import { buildPayload, formInitialValue } from '../../lib/forms'
import { formatOptionLabel } from '../../lib/labels'
import { dateBRToApi, maskCep, maskCnpj, maskCpf, maskDateBR, maskPhoneBR, maskTime, todayInputValue } from '../../lib/masks'

export function JsonForm({
  title,
  fields,
  onSubmit,
  onCancel,
  submitting = false,
  initialData,
  submitLabel = 'Salvar',
  successMessage = 'Salvo.',
}: {
  title: string
  fields: FieldDef[]
  onSubmit: (payload: Record<string, unknown>) => Promise<unknown> | unknown
  onCancel?: () => void
  submitting?: boolean
  initialData?: Record<string, unknown>
  submitLabel?: string
  successMessage?: string
}) {
  const initial = useMemo(
    () => Object.fromEntries(fields.map(field => [field.name, formInitialValue(field, initialData)])),
    [fields, initialData]
  )
  const [data, setData] = useState<Record<string, unknown>>(initial)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => setData(initial), [initial])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setMessage('')
    try {
      const today = todayInputValue()
      const invalidDate = fields.find(field =>
        field.disallowPast &&
        typeof data[field.name] === 'string' &&
        Boolean(dateBRToApi(String(data[field.name]))) &&
        String(dateBRToApi(String(data[field.name]))) < today
      )
      if (invalidDate) {
        throw new Error(`${invalidDate.label} não pode ser anterior a hoje.`)
      }
      await onSubmit(buildPayload(fields, data))
      setMessage(successMessage)
      setData(initial)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao salvar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel">
      <h2>{title}</h2>
      <form className="form-grid" onSubmit={submit}>
        {fields.map(field => (
          <label key={field.name} className={field.type === 'textarea' ? 'span-two' : ''}>
            <span>{field.label}{field.required ? <span aria-hidden="true"> *</span> : ''}</span>
            {renderInput(field, data[field.name], value => setData(current => ({ ...current, [field.name]: value })))}
          </label>
        ))}
        <div className="form-actions span-two">
          {onCancel && <button type="button" className="secondary-button" onClick={onCancel}>Cancelar</button>}
          <button type="submit" disabled={busy || submitting}>{busy || submitting ? 'Salvando...' : submitLabel}</button>
        </div>
      </form>
      {message && (
        <div className={message === successMessage ? 'notice' : 'notice error'} role={message === successMessage ? 'status' : 'alert'}>
          {message}
        </div>
      )}
    </section>
  )
}

export function renderInput(field: FieldDef, value: unknown, onChange: (value: unknown) => void) {
  if (field.type === 'textarea') {
    return <textarea value={String(value ?? '')} onChange={event => onChange(event.target.value)} placeholder={field.placeholder} required={field.required} />
  }
  if (field.type === 'select' || field.selectOptions?.length) {
    const options = field.selectOptions ?? (field.options ?? []).map(option => ({ value: option, label: formatOptionLabel(option) }))
    return (
      <select value={String(value ?? '')} onChange={event => onChange(event.target.value)} required={field.required}>
        <option value="">Selecione</option>
        {options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    )
  }
  if (field.type === 'checkbox') {
    return <input type="checkbox" checked={Boolean(value)} onChange={event => onChange(event.target.checked)} />
  }
  if (field.name === 'cnpj') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={18}
        value={String(value ?? '')}
        onChange={event => onChange(maskCnpj(event.target.value))}
        placeholder="00.000.000/0000-00"
        required={field.required}
      />
    )
  }
  if (field.name === 'cpf') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={14}
        value={String(value ?? '')}
        onChange={event => onChange(maskCpf(event.target.value))}
        placeholder="000.000.000-00"
        required={field.required}
      />
    )
  }
  if (field.name === 'zipCode') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={9}
        value={String(value ?? '')}
        onChange={event => onChange(maskCep(event.target.value))}
        placeholder="00000-000"
        required={field.required}
      />
    )
  }
  if (field.name === 'phone') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={12}
        value={String(value ?? '')}
        onChange={event => onChange(maskPhoneBR(event.target.value))}
        placeholder="31 9999-2702"
        required={field.required}
      />
    )
  }
  if (field.type === 'time') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={5}
        value={String(value ?? '')}
        onChange={event => onChange(maskTime(event.target.value))}
        placeholder="14:00"
        required={field.required}
      />
    )
  }
  if (field.name === 'birthDate' || field.type === 'date') {
    return (
      <input
        type="text"
        inputMode="numeric"
        maxLength={10}
        value={String(value ?? '')}
        onChange={event => onChange(maskDateBR(event.target.value))}
        placeholder="DD/MM/AAAA"
        required={field.required}
      />
    )
  }
  return (
    <input
      type={field.type ?? 'text'}
      step={field.step}
      value={String(value ?? '')}
      onChange={event => onChange(event.target.value)}
      placeholder={field.placeholder}
      required={field.required}
    />
  )
}
