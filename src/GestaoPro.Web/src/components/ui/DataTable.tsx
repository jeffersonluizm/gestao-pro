import { LookupData, Row } from '../../domain/types'
import { formatCell, normalizeId } from '../../lib/format'
import { labels } from '../../lib/labels'
import { EmptyState } from './States'

export interface RowAction {
  label: string
  onClick: (row: Row) => void | Promise<void>
  variant?: 'danger' | 'secondary'
  // Mostra a ação só nas linhas em que ela faz sentido (ex.: cancelar só reserva futura).
  visible?: (row: Row) => boolean
}

export function DataTable({
  rows,
  columns,
  lookups,
  onEdit,
  onDelete,
  deleteLabel = 'Remover',
  actions = [],
  caption,
  emptyTitle = 'Nenhum registro ainda.',
  emptyHint,
}: {
  rows: Row[]
  columns?: string[]
  lookups?: LookupData
  onEdit?: (row: Row) => void | Promise<void>
  onDelete?: (row: Row) => void | Promise<void>
  deleteLabel?: string
  actions?: RowAction[]
  caption?: string
  emptyTitle?: string
  emptyHint?: string
}) {
  const visibleColumns = columns?.length
    ? columns
    : Array.from(new Set(rows.flatMap(row => Object.keys(row)))).slice(0, 6)
  const allActions: RowAction[] = [
    ...(onEdit ? [{ label: 'Editar', onClick: onEdit, variant: 'secondary' as const }] : []),
    ...actions,
    ...(onDelete ? [{ label: deleteLabel, onClick: onDelete, variant: 'danger' as const }] : []),
  ]

  if (!rows.length) return <EmptyState title={emptyTitle}>{emptyHint}</EmptyState>

  return (
    <div className="table-wrap">
      <table>
        {caption && <caption>{caption}</caption>}
        <thead>
          <tr>
            {visibleColumns.map(column => <th key={column} scope="col">{labels[column] ?? column}</th>)}
            {allActions.length > 0 && <th scope="col">Ações</th>}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={normalizeId(row) || index}>
              {visibleColumns.map(column => <td key={column}>{formatCell(column, row[column], lookups)}</td>)}
              {allActions.length > 0 && (
                <td>
                  <div className="row-actions">
                    {allActions.filter(action => action.visible?.(row) ?? true).map(action => (
                      <button
                        key={action.label}
                        type="button"
                        className={action.variant === 'danger' ? 'danger' : action.variant === 'secondary' ? 'secondary-button' : ''}
                        onClick={() => action.onClick(row)}
                      >
                        {action.label}
                      </button>
                    ))}
                  </div>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
