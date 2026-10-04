import { useEffect, useState } from 'react'
import { requestJson } from '../../api/http'
import { JsonForm } from '../../components/form/ResourceForm'
import { DataTable } from '../../components/ui/DataTable'
import { PanelHeader } from '../../components/ui/PanelHeader'
import { AppSession, LookupData, ModuleDef } from '../../domain/types'
import { formatCurrency, formatDateOnly, formatValue, normalizeId } from '../../lib/format'
import { createSuccessMessages, isSuccessMessage, updateSuccessMessages } from '../../lib/labels'

export function billOptionLabel(bill: Record<string, unknown>) {
  const dueDate = typeof bill.dueDate === 'string' ? formatDateOnly(bill.dueDate) : '-'
  return `${String(bill.description ?? 'Cobrança')} - ${formatCurrency(bill.amount)} - venc. ${dueDate} - ${formatValue(bill.status)}`
}

// A API só lista pagamentos por cobrança (GET /api/Payments/bill/{billId}) e cobranças por morador:
// o administrador escolhe o morador e depois a cobrança; o morador já vê as próprias cobranças.
export function PaymentsPage({ module, canManage, session, lookups }: { module: ModuleDef; canManage: boolean; session: AppSession; lookups: LookupData }) {
  const [userId, setUserId] = useState(canManage ? '' : session.userId)
  const [bills, setBills] = useState<Record<string, unknown>[]>([])
  const [billId, setBillId] = useState('')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [message, setMessage] = useState('')
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null)
  const fields = module.fields ?? []
  const selectedBill = bills.find(bill => normalizeId(bill) === billId)

  async function fetchBills(targetUserId: string) {
    const data = await requestJson<Record<string, unknown>[]>(`/api/Bills/user/${encodeURIComponent(targetUserId)}`)
    setBills(Array.isArray(data) ? data : [])
  }

  async function loadPayments(targetBillId: string) {
    setRows([])
    if (!targetBillId) return
    try {
      const data = await requestJson<Record<string, unknown>[]>(`/api/Payments/bill/${encodeURIComponent(targetBillId)}`)
      setRows(Array.isArray(data) ? data : [])
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar os pagamentos.')
    }
  }

  async function selectUser(nextUserId: string) {
    setUserId(nextUserId)
    setBills([])
    setBillId('')
    setRows([])
    setEditingRow(null)
    setMessage('')
    if (!nextUserId) return
    try {
      await fetchBills(nextUserId)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar as cobranças.')
    }
  }

  async function selectBill(nextBillId: string) {
    setBillId(nextBillId)
    setEditingRow(null)
    setMessage('')
    await loadPayments(nextBillId)
  }

  useEffect(() => {
    if (!canManage && session.userId) selectUser(session.userId)
  }, [canManage, session.userId])

  async function create(payload: Record<string, unknown>) {
    if (!module.createEndpoint || !billId) return
    await requestJson(module.createEndpoint, {
      method: 'POST',
      body: JSON.stringify({ billId, ...payload }),
    })
    // A API marca a cobrança como paga ao registrar o pagamento: recarrega as duas listas.
    await Promise.all([fetchBills(userId), loadPayments(billId)])
    setMessage(createSuccessMessages.payments ?? 'Registro criado com sucesso.')
  }

  async function update(payload: Record<string, unknown>) {
    const id = editingRow ? normalizeId(editingRow) : ''
    if (!id || !module.createEndpoint) return
    await requestJson(`${module.createEndpoint}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
    setEditingRow(null)
    await loadPayments(billId)
    setMessage(updateSuccessMessages.payments ?? 'Registro atualizado com sucesso.')
  }

  return (
    <div className={canManage ? 'panel-grid' : 'panel-grid read-only'}>
      {canManage && billId && (
        <JsonForm title="Novo pagamento" fields={fields} onSubmit={create} submitLabel="Registrar pagamento" successMessage="" />
      )}
      {canManage && editingRow && (
        <JsonForm
          key={normalizeId(editingRow)}
          title="Editar pagamento"
          fields={fields}
          initialData={editingRow}
          submitLabel="Atualizar"
          onSubmit={update}
          successMessage=""
          onCancel={() => setEditingRow(null)}
        />
      )}
      <section className="panel">
        <PanelHeader title="Pagamentos" onRefresh={billId ? () => loadPayments(billId) : undefined} />
        <div className="form-grid">
          {canManage && (
            <label>
              <span>Morador</span>
              <select value={userId} onChange={event => selectUser(event.target.value)}>
                <option value="">Selecione</option>
                {lookups.users.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
          )}
          <label>
            <span>Cobrança</span>
            <select value={billId} onChange={event => selectBill(event.target.value)} disabled={!bills.length}>
              <option value="">{userId && !bills.length ? 'Nenhuma cobrança' : 'Selecione'}</option>
              {bills.map(bill => <option key={normalizeId(bill)} value={normalizeId(bill)}>{billOptionLabel(bill)}</option>)}
            </select>
          </label>
        </div>
        {selectedBill && (
          <div className="notice" style={{ marginTop: 8 }}>
            <strong>Cobrança:</strong> {billOptionLabel(selectedBill)}
          </div>
        )}
        {message && <div className={isSuccessMessage(message) ? 'notice' : 'notice error'}>{message}</div>}
        <DataTable
          rows={rows}
          columns={module.columns}
          lookups={lookups}
          onEdit={canManage ? row => setEditingRow(row) : undefined}
        />
      </section>
    </div>
  )
}
