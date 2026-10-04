import { useEffect, useState } from 'react'
import { requestJson } from '../../api/http'
import { isFrontDesk } from '../../auth/session'
import { JsonForm } from '../../components/form/ResourceForm'
import { DataTable } from '../../components/ui/DataTable'
import { PanelHeader } from '../../components/ui/PanelHeader'
import { AppSession, LookupData, ModuleDef } from '../../domain/types'
import { ReservationCalendar } from './ReservationCalendar'
import { DeliveryActions, VisitorActions } from './ResourceActions'
import { normalizeId } from '../../lib/format'
import { fieldsForSession, withLookupOptions } from '../../lib/forms'
import { createSuccessMessages, deleteConfirmationMessage, deleteSuccessMessages, isSuccessMessage, updateSuccessMessages } from '../../lib/labels'
import { dateInputToIso } from '../../lib/masks'

export function filterRowsByMonth(rows: Record<string, unknown>[], month: Date, fieldName: string) {
  const monthKey = `${month.getFullYear()}-${String(month.getMonth() + 1).padStart(2, '0')}`
  return rows.filter(row => String(row[fieldName] ?? '').slice(0, 7) === monthKey)
}

export function ResourcePage({
  module,
  canManage,
  session,
  lookups,
  onChanged,
}: {
  module: ModuleDef
  canManage: boolean
  session: AppSession
  lookups: LookupData
  onChanged?: () => void | Promise<void>
}) {
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null)
  const [showAssemblyHistory, setShowAssemblyHistory] = useState(false)
  const [reservationMonth, setReservationMonth] = useState(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  })
  const formFields = withLookupOptions(fieldsForSession(module, canManage), lookups)
  const editFields = module.editFields ? withLookupOptions(module.editFields, lookups) : formFields
  const canCreate = Boolean(formFields?.length && module.createEndpoint && (canManage || ['reservations', 'visitors'].includes(module.id)))
  const isReadOnly = !canCreate
  const visibleRows = module.id === 'reservations' ? filterRowsByMonth(rows, reservationMonth, 'date') : rows

  async function load() {
    if (!module.listEndpoint) return
    setLoading(true)
    setMessage('')
    try {
      const listEndpoint = module.id === 'assembly' && showAssemblyHistory
        ? `${module.listEndpoint}?history=true`
        : module.listEndpoint
      const data = await requestJson<Record<string, unknown>[]>(listEndpoint)
      setRows(Array.isArray(data) ? data : [])
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [module.id, showAssemblyHistory])

  async function create(payload: Record<string, unknown>) {
    if (!module.createEndpoint) return
    const body = { ...payload }
    if (module.id === 'reservations' && !canManage) {
      body.userId = session.userId
      body.condominiumId = session.condominiumId
      body.status = 'pendente'
    }
    if (module.id === 'visitors' && !canManage) {
      body.residentId = session.userId
      body.condominiumId = session.condominiumId
    }
    if (module.id === 'assembly') {
      body.createdBy = session.userId
      body.startDate = dateInputToIso(body.startDate)
      body.endDate = dateInputToIso(body.endDate, true)
      body.scheduledDate = body.startDate
    }
    const created = await requestJson<Record<string, unknown>>(module.createEndpoint, {
      method: 'POST',
      headers: module.createHeaders?.(body),
      body: JSON.stringify(body),
    })
    await load()
    await onChanged?.()
    if (module.id === 'users' && created?.registrationNumber) {
      setMessage(`Usuário criado com sucesso. Login com o e-mail: ${created.registrationNumber}`)
      return
    }
    setMessage(createSuccessMessages[module.id] ?? 'Registro criado com sucesso.')
  }

  async function update(payload: Record<string, unknown>) {
    const id = editingRow ? normalizeId(editingRow) : ''
    const updateEndpoint = module.listEndpoint || module.createEndpoint
    if (!id || !updateEndpoint) return
    const body = { ...payload }
    if (module.id === 'assembly') {
      body.createdBy = String(editingRow?.createdBy ?? session.userId)
      body.startDate = dateInputToIso(body.startDate)
      body.endDate = dateInputToIso(body.endDate, true)
      body.scheduledDate = body.startDate
    }
    await requestJson(`${updateEndpoint}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
    setEditingRow(null)
    await load()
    await onChanged?.()
    setMessage(updateSuccessMessages[module.id] ?? 'Registro atualizado com sucesso.')
  }

  async function remove(row: Record<string, unknown>) {
    const id = normalizeId(row)
    const deleteEndpoint = module.listEndpoint || module.createEndpoint
    if (!id || !deleteEndpoint) {
      setMessage('Não foi possível identificar o registro para remoção.')
      return
    }
    if (!window.confirm(deleteConfirmationMessage(module.id))) return
    try {
      await requestJson(`${deleteEndpoint}/${id}`, { method: 'DELETE' })
      await load()
      await onChanged?.()
      setMessage(deleteSuccessMessages[module.id] ?? 'Registro removido com sucesso.')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : String(error))
    }
  }

  return (
    <div className={isReadOnly ? 'panel-grid read-only' : 'panel-grid'}>
      {canCreate && formFields && (
        <JsonForm
          title={module.id === 'reservations' && !canManage ? 'Nova reserva' : 'Novo registro'}
          fields={formFields}
          onSubmit={create}
          successMessage=""
        />
      )}
      {canManage && editFields && editingRow && (
        <JsonForm
          key={normalizeId(editingRow)}
          title="Editar registro"
          fields={editFields}
          initialData={editingRow}
          submitLabel="Atualizar"
          onSubmit={update}
          successMessage=""
          onCancel={() => setEditingRow(null)}
        />
      )}
      <section className="panel">
        <PanelHeader title="Registros" loading={loading} onRefresh={module.listEndpoint ? load : undefined} />
        {module.id === 'assembly' && (
          <div className="filter-row">
            <button type="button" className={!showAssemblyHistory ? '' : 'secondary-button'} onClick={() => setShowAssemblyHistory(false)}>
              Assembleias atuais
            </button>
            <button type="button" className={showAssemblyHistory ? '' : 'secondary-button'} onClick={() => setShowAssemblyHistory(true)}>
              Histórico
            </button>
          </div>
        )}
        {message && (
          <div className={isSuccessMessage(message) ? 'notice' : 'notice error'}>
            {message}
          </div>
        )}
        {module.id === 'reservations' && (
          <ReservationCalendar
            rows={rows}
            lookups={lookups}
            currentMonth={reservationMonth}
            onMonthChange={setReservationMonth}
          />
        )}
        <DataTable
          rows={visibleRows}
          columns={module.columns}
          lookups={lookups}
          onEdit={canManage && editFields ? row => setEditingRow(row) : undefined}
          onDelete={canManage && (module.listEndpoint || module.createEndpoint) && !module.noDelete ? remove : undefined}
          deleteLabel={module.id === 'users' ? 'Desativar' : undefined}
        />
        {(canManage || isFrontDesk(session)) && module.id === 'visitors' && <VisitorActions onDone={load} lookups={lookups} />}
        {canManage && module.id === 'deliveries' && <DeliveryActions onDone={load} lookups={lookups} />}
      </section>
    </div>
  )
}
