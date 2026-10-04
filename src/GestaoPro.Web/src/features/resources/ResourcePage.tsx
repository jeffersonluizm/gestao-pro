import { useEffect, useState } from 'react'
import { requestJson } from '../../api/http'
import { isFrontDesk } from '../../auth/session'
import { JsonForm } from '../../components/form/ResourceForm'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { DataTable, RowAction } from '../../components/ui/DataTable'
import { PanelHeader } from '../../components/ui/PanelHeader'
import { ErrorState, LoadingState } from '../../components/ui/States'
import { useToast } from '../../components/ui/Toast'
import { AppSession, LookupData, ModuleDef, Row } from '../../domain/types'
import { normalizeId } from '../../lib/format'
import { fieldsForSession, withLookupOptions, withoutSessionFields } from '../../lib/forms'
import { dateInputToIso, todayInputValue } from '../../lib/masks'
import { ReservationCalendar } from './ReservationCalendar'
import { DeliveryActions, VisitorActions } from './ResourceActions'
import { useResourceCrud } from './useResourceCrud'

export function filterRowsByMonth(rows: Row[], month: Date, fieldName: string) {
  const monthKey = `${month.getFullYear()}-${String(month.getMonth() + 1).padStart(2, '0')}`
  return rows.filter(row => String(row[fieldName] ?? '').slice(0, 7) === monthKey)
}

// Tela de cadastro guiada por configuração (features/modules.ts): formulário, lista e ações por linha.
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
  const toast = useToast()
  const confirm = useConfirm()
  const [editingRow, setEditingRow] = useState<Row | null>(null)
  const [showAssemblyHistory, setShowAssemblyHistory] = useState(false)
  const [reservationMonth, setReservationMonth] = useState(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  })
  const listEndpoint = module.id === 'assembly' && showAssemblyHistory && module.listEndpoint
    ? `${module.listEndpoint}?history=true`
    : module.listEndpoint
  const crud = useResourceCrud(module, session, { list: listEndpoint })
  const formFields = withLookupOptions(withoutSessionFields(fieldsForSession(module, canManage)), lookups)
  const editFields = module.editFields ? withLookupOptions(withoutSessionFields(module.editFields), lookups) : formFields
  const canCreate = Boolean(formFields?.length && module.createEndpoint && (canManage || ['reservations', 'visitors'].includes(module.id)))
  const visibleRows = module.id === 'reservations' ? filterRowsByMonth(crud.rows, reservationMonth, 'date') : crud.rows
  const { load } = crud

  useEffect(() => { load() }, [load])

  async function afterChange() {
    await load()
    await onChanged?.()
  }

  async function create(payload: Row) {
    const extra: Row = {}
    if (module.id === 'reservations' && !canManage) Object.assign(extra, { userId: session.userId, status: 'pendente' })
    if (module.id === 'visitors' && !canManage) Object.assign(extra, { residentId: session.userId })
    if (module.id === 'assembly') Object.assign(extra, assemblyDates(payload, session.userId))
    await crud.create(payload, extra)
    await afterChange()
  }

  async function update(payload: Row) {
    if (!editingRow) return
    const extra = module.id === 'assembly' ? assemblyDates(payload, String(editingRow.createdBy ?? session.userId)) : {}
    await crud.update(editingRow, payload, extra)
    setEditingRow(null)
    await afterChange()
  }

  async function remove(row: Row) {
    if (await crud.remove(row)) await afterChange()
  }

  // Morador cancela a própria reserva futura (a API confere o dono).
  async function cancelOwnReservation(row: Row) {
    const ok = await confirm({
      title: 'Cancelar reserva',
      message: `A reserva de ${String(row.date ?? '').slice(0, 10).split('-').reverse().join('/')} será cancelada e o horário fica livre para outros moradores.`,
      confirmLabel: 'Cancelar reserva',
      cancelLabel: 'Manter reserva',
      destructive: true,
    })
    if (!ok) return
    try {
      await requestJson(`/api/reservations/${normalizeId(row)}`, { method: 'DELETE' })
      toast.success('Reserva cancelada.')
      await afterChange()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Não foi possível cancelar a reserva.')
    }
  }

  // Gestão: senha volta a ser o código do condomínio e a pessoa troca no próximo acesso.
  async function resetPassword(row: Row) {
    const ok = await confirm({
      title: 'Redefinir senha',
      message: `A senha de ${String(row.name ?? 'este usuário')} volta a ser o código do condomínio. No próximo acesso, a pessoa entra com esse código e cria uma senha nova.`,
      confirmLabel: 'Redefinir senha',
    })
    if (!ok) return
    try {
      await requestJson(`/api/Users/${normalizeId(row)}/reset-password`, { method: 'PATCH' })
      toast.success('Senha redefinida. Avise a pessoa para entrar com o código do condomínio.')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Não foi possível redefinir a senha.')
    }
  }

  const actions: RowAction[] = []
  if (module.id === 'users' && canManage) actions.push({ label: 'Redefinir senha', onClick: resetPassword, variant: 'secondary' })
  if (module.id === 'reservations' && !canManage) {
    actions.push({
      label: 'Cancelar reserva',
      onClick: cancelOwnReservation,
      variant: 'danger',
      visible: row => String(row.userId ?? '') === session.userId
        && String(row.date ?? '').slice(0, 10) >= todayInputValue()
        && !['cancelada', 'rejeitada'].includes(String(row.status ?? '')),
    })
  }

  return (
    <div className={canCreate ? 'panel-grid' : 'panel-grid read-only'}>
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
      <section className="panel" aria-busy={crud.loading}>
        <PanelHeader title="Registros" loading={crud.loading} onRefresh={module.listEndpoint ? () => load() : undefined} />
        {module.id === 'assembly' && (
          <div className="filter-row" role="group" aria-label="Período das assembleias">
            <button type="button" aria-pressed={!showAssemblyHistory} className={!showAssemblyHistory ? '' : 'secondary-button'} onClick={() => setShowAssemblyHistory(false)}>
              Assembleias atuais
            </button>
            <button type="button" aria-pressed={showAssemblyHistory} className={showAssemblyHistory ? '' : 'secondary-button'} onClick={() => setShowAssemblyHistory(true)}>
              Histórico
            </button>
          </div>
        )}
        {crud.error && <ErrorState message={crud.error} onRetry={() => load()} />}
        {module.id === 'reservations' && (
          <ReservationCalendar rows={crud.rows} lookups={lookups} currentMonth={reservationMonth} onMonthChange={setReservationMonth} />
        )}
        {crud.loading && !crud.rows.length ? <LoadingState /> : (
          <DataTable
            rows={visibleRows}
            columns={module.columns}
            lookups={lookups}
            caption={module.title}
            emptyTitle={module.id === 'reservations' ? 'Nenhuma reserva neste mês.' : 'Nenhum registro ainda.'}
            emptyHint={canCreate ? 'Use o formulário ao lado para cadastrar o primeiro.' : undefined}
            onEdit={canManage && editFields ? row => setEditingRow(row) : undefined}
            onDelete={canManage && (module.listEndpoint || module.createEndpoint) && !module.noDelete ? remove : undefined}
            deleteLabel={module.id === 'users' ? 'Desativar' : undefined}
            actions={actions}
          />
        )}
        {(canManage || isFrontDesk(session)) && module.id === 'visitors' && <VisitorActions onDone={afterChange} lookups={lookups} />}
        {(canManage || isFrontDesk(session)) && module.id === 'deliveries' && <DeliveryActions onDone={afterChange} lookups={lookups} />}
      </section>
    </div>
  )
}

function assemblyDates(payload: Row, createdBy: string): Row {
  const startDate = dateInputToIso(payload.startDate)
  return { createdBy, startDate, endDate: dateInputToIso(payload.endDate, true), scheduledDate: startDate }
}
