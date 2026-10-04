import { LookupData } from '../../domain/types'
import { formatCell, normalizeId } from '../../lib/format'
import { labels } from '../../lib/labels'

export function DataTable({
  rows,
  columns,
  lookups,
  onEdit,
  onDelete,
  deleteLabel = 'Remover',
}: {
  rows: Record<string, unknown>[]
  columns?: string[]
  lookups?: LookupData
  onEdit?: (row: Record<string, unknown>) => void | Promise<void>
  onDelete?: (row: Record<string, unknown>) => void | Promise<void>
  deleteLabel?: string
}) {
  const visibleColumns = columns?.length
    ? columns
    : Array.from(new Set(rows.flatMap(row => Object.keys(row)))).slice(0, 6)

  if (!rows.length) return <div className="empty-state">Nenhum registro carregado.</div>

  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            {visibleColumns.map(column => <th key={column}>{labels[column] ?? column}</th>)}
            {(onEdit || onDelete) && <th>Ações</th>}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={normalizeId(row) || index}>
              {visibleColumns.map(column => <td key={column}>{formatCell(column, row[column], lookups)}</td>)}
              {(onEdit || onDelete) && (
                <td>
                  <div className="row-actions">
                    {onEdit && <button type="button" onClick={() => onEdit(row)}>Editar</button>}
                    {onDelete && <button className="danger" type="button" onClick={() => onDelete(row)}>{deleteLabel}</button>}
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
