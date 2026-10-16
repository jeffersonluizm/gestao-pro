import { createContext, ReactNode, useCallback, useContext, useMemo, useRef, useState } from 'react'

type ToastKind = 'success' | 'error'
interface ToastItem { id: number; kind: ToastKind; message: string }
interface ToastApi { success: (message: string) => void; error: (message: string) => void }

const ToastContext = createContext<ToastApi | null>(null)

// Avisos curtos de resultado (salvo, removido, erro de ação em lote). A região é anunciada ao leitor de tela.
export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([])
  const nextId = useRef(1)

  const dismiss = useCallback((id: number) => setItems(current => current.filter(item => item.id !== id)), [])
  const push = useCallback((kind: ToastKind, message: string) => {
    const id = nextId.current++
    setItems(current => [...current.slice(-2), { id, kind, message }])
    window.setTimeout(() => dismiss(id), kind === 'error' ? 8000 : 5000)
  }, [dismiss])

  const api = useMemo<ToastApi>(() => ({
    success: message => push('success', message),
    error: message => push('error', message),
  }), [push])

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toast-region" role="status" aria-live="polite">
        {items.map(item => (
          <div key={item.id} className={item.kind === 'error' ? 'toast error' : 'toast'}>
            <p>{item.message}</p>
            <button type="button" onClick={() => dismiss(item.id)} aria-label="Fechar aviso">×</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) throw new Error('useToast precisa estar dentro de ToastProvider.')
  return context
}
