import { useState } from 'react'
import { requestJson } from '../../api/http'
import { useToast } from '../../components/ui/Toast'
import { LookupData } from '../../domain/types'

// Portaria: entrada e saída de visitante, com retorno visível do resultado.
export function VisitorActions({ onDone, lookups }: { onDone: () => void | Promise<void>; lookups: LookupData }) {
  const toast = useToast()
  const [visitorId, setVisitorId] = useState('')
  const [busy, setBusy] = useState(false)

  async function post(action: 'check-in' | 'check-out') {
    if (!visitorId) {
      toast.error('Selecione o visitante.')
      return
    }
    setBusy(true)
    try {
      await requestJson(`/api/Visitor/${action}/${visitorId}`, { method: 'POST' })
      toast.success(action === 'check-in' ? 'Entrada registrada.' : 'Saída registrada.')
      await onDone()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Não foi possível registrar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="inline-actions" role="group" aria-label="Entrada e saída de visitante">
      <label className="field">
        <span className="field-label">Visitante</span>
        <select value={visitorId} onChange={event => setVisitorId(event.target.value)}>
          <option value="">Selecione o visitante</option>
          {lookups.visitors.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
      </label>
      <button type="button" disabled={busy} onClick={() => post('check-in')}>Entrada</button>
      <button type="button" className="secondary-button" disabled={busy} onClick={() => post('check-out')}>Saída</button>
    </div>
  )
}

// Portaria e gestão: registrar a retirada de uma encomenda.
export function DeliveryActions({ onDone, lookups }: { onDone: () => void | Promise<void>; lookups: LookupData }) {
  const toast = useToast()
  const [id, setId] = useState('')
  const [pickedUpBy, setPickedUpBy] = useState('')
  const [busy, setBusy] = useState(false)

  async function pickup() {
    if (!id) return
    setBusy(true)
    try {
      await requestJson(`/api/Delivery/${id}/pickup?pickedUpBy=${encodeURIComponent(pickedUpBy)}`, { method: 'PUT' })
      toast.success('Retirada registrada.')
      setId('')
      setPickedUpBy('')
      await onDone()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Não foi possível registrar a retirada.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="inline-actions stacked" role="group" aria-label="Retirada de encomenda">
      <div>
        <label className="field">
          <span className="field-label">Encomenda</span>
          <select value={id} onChange={event => setId(event.target.value)}>
            <option value="">Selecione a encomenda</option>
            {lookups.deliveries.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>
        <label className="field">
          <span className="field-label">Retirado por</span>
          <input value={pickedUpBy} onChange={event => setPickedUpBy(event.target.value)} placeholder="Nome de quem retirou" />
        </label>
        <button type="button" onClick={pickup} disabled={!id || busy}>Registrar retirada</button>
      </div>
    </div>
  )
}
