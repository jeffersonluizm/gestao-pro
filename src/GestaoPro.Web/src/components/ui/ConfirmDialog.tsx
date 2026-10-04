import { createContext, ReactNode, useCallback, useContext, useEffect, useRef, useState } from 'react'

export interface ConfirmOptions {
  title: string
  message: string
  confirmLabel: string
  cancelLabel?: string
  destructive?: boolean
}

type Confirm = (options: ConfirmOptions) => Promise<boolean>
const ConfirmContext = createContext<Confirm | null>(null)

// Diálogo próprio de confirmação (substitui o window.confirm): foco no botão seguro, Esc cancela,
// o foco volta para quem abriu. Ações destrutivas usam o botão vermelho.
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<(ConfirmOptions & { resolve: (ok: boolean) => void }) | null>(null)
  const cancelRef = useRef<HTMLButtonElement>(null)
  const openerRef = useRef<Element | null>(null)

  const confirm = useCallback<Confirm>(options => new Promise(resolve => {
    openerRef.current = document.activeElement
    setPending({ ...options, resolve })
  }), [])

  const close = useCallback((ok: boolean) => {
    pending?.resolve(ok)
    setPending(null)
    if (openerRef.current instanceof HTMLElement) openerRef.current.focus()
  }, [pending])

  useEffect(() => {
    if (!pending) return
    cancelRef.current?.focus()
    const onKey = (event: KeyboardEvent) => { if (event.key === 'Escape') close(false) }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [pending, close])

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {pending && (
        <div className="dialog-backdrop" onClick={() => close(false)}>
          <div
            className="confirm-dialog card"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="confirm-title"
            aria-describedby="confirm-message"
            onClick={event => event.stopPropagation()}
          >
            <h2 id="confirm-title">{pending.title}</h2>
            <p id="confirm-message">{pending.message}</p>
            <div className="form-actions">
              <button ref={cancelRef} type="button" className="secondary-button" onClick={() => close(false)}>
                {pending.cancelLabel ?? 'Voltar'}
              </button>
              <button type="button" className={pending.destructive ? 'danger' : ''} onClick={() => close(true)}>
                {pending.confirmLabel}
              </button>
            </div>
          </div>
        </div>
      )}
    </ConfirmContext.Provider>
  )
}

export function useConfirm() {
  const context = useContext(ConfirmContext)
  if (!context) throw new Error('useConfirm precisa estar dentro de ConfirmProvider.')
  return context
}
