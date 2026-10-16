import { useEffect, useState } from 'react'
import { SERVER_READY_EVENT, SERVER_SLOW_EVENT } from '../../api/http'

// Aviso de "servidor acordando" (hospedagem gratuita leva de 30 a 60 s na primeira requisição).
export function ServerWakeNotice() {
  const [waking, setWaking] = useState(false)

  useEffect(() => {
    const onSlow = () => setWaking(true)
    const onReady = () => setWaking(false)
    window.addEventListener(SERVER_SLOW_EVENT, onSlow)
    window.addEventListener(SERVER_READY_EVENT, onReady)
    return () => {
      window.removeEventListener(SERVER_SLOW_EVENT, onSlow)
      window.removeEventListener(SERVER_READY_EVENT, onReady)
    }
  }, [])

  if (!waking) return null
  return (
    <div className="wake-notice" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <p>
        <strong>Acordando o servidor de demonstração.</strong> Ele fica em hospedagem gratuita e dorme quando ninguém usa:
        a primeira resposta pode levar até 1 minuto. As próximas são rápidas.
      </p>
    </div>
  )
}
