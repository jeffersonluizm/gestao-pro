export function PanelHeader({ title, loading, onRefresh }: { title: string; loading?: boolean; onRefresh?: () => void }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      {onRefresh && <button type="button" onClick={onRefresh}>{loading ? 'Carregando...' : 'Atualizar'}</button>}
    </div>
  )
}
