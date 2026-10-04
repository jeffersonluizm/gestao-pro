import { ReactNode } from 'react'

// Estados de lista: carregando (esqueleto), vazio (com orientação) e erro (com nova tentativa).
export function LoadingState({ label = 'Carregando...', lines = 3 }: { label?: string; lines?: number }) {
  return (
    <div role="status" aria-live="polite" className="form-stack">
      <span className="visually-hidden">{label}</span>
      {Array.from({ length: lines }, (_, index) => <div key={index} className="skeleton" style={{ height: 44 }} />)}
    </div>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      {children && <span>{children}</span>}
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="notice error" role="alert">
      <p>{message}</p>
      {onRetry && <button type="button" className="secondary-button" onClick={onRetry} style={{ marginTop: 8 }}>Tentar de novo</button>}
    </div>
  )
}
