import { Component, ReactNode } from 'react'

// Erro inesperado de renderização: mensagem amigável em vez de tela branca (o detalhe vai para o console).
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }

  static getDerivedStateFromError() {
    return { failed: true }
  }

  componentDidCatch(error: unknown) {
    console.error('Erro inesperado no painel:', error)
  }

  render() {
    if (!this.state.failed) return this.props.children
    return (
      <main className="system-page">
        <div className="card" role="alert">
          <p className="system-code" aria-hidden="true">!</p>
          <h1>Algo deu errado</h1>
          <p className="muted">A tela não pôde ser exibida. Recarregue a página; se continuar, tente de novo em alguns minutos.</p>
          <button type="button" onClick={() => window.location.assign('/')}>Voltar ao início</button>
        </div>
      </main>
    )
  }
}
