import { useState } from 'react'
import { requestJson } from '../../api/http'
import { LookupData } from '../../domain/types'

export function VisitorActions({ onDone, lookups }: { onDone: () => void; lookups: LookupData }) {
  const [visitorId, setVisitorId] = useState('')
  async function post(action: 'check-in' | 'check-out') {
    await requestJson(`/api/Visitor/${action}/${visitorId}`, { method: 'POST' })
    onDone()
  }
  return (
    <div className="inline-actions">
      <select value={visitorId} onChange={event => setVisitorId(event.target.value)}>
        <option value="">Selecione o visitante</option>
        {lookups.visitors.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
      <button type="button" onClick={() => post('check-in')}>Entrada</button>
      <button type="button" onClick={() => post('check-out')}>Saída</button>
    </div>
  )
}

export function DeliveryActions({ onDone, lookups }: { onDone: () => void; lookups: LookupData }) {
  const [id, setId] = useState('')
  const [pickedUpBy, setPickedUpBy] = useState('')

  async function pickup() {
    if (!id) return
    await requestJson(`/api/Delivery/${id}/pickup?pickedUpBy=${encodeURIComponent(pickedUpBy)}`, { method: 'PUT' })
    onDone()
  }

  return (
    <div className="inline-actions stacked">
      <div>
        <select value={id} onChange={event => setId(event.target.value)}>
          <option value="">Selecione a encomenda</option>
          {lookups.deliveries.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
        <input value={pickedUpBy} onChange={event => setPickedUpBy(event.target.value)} placeholder="Retirado por" />
        <button type="button" onClick={pickup} disabled={!id}>Retirada</button>
      </div>
    </div>
  )
}
