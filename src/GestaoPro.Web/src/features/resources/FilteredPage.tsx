import { ReactNode, useEffect, useState } from 'react'
import { requestJson } from '../../api/http'
import { JsonForm } from '../../components/form/ResourceForm'
import { DataTable } from '../../components/ui/DataTable'
import { LookupData, LookupKey, ModuleDef } from '../../domain/types'
import { normalizeId } from '../../lib/format'
import { withLookupOptions } from '../../lib/forms'
import { createSuccessMessages, deleteConfirmationMessage, deleteSuccessMessages, isSuccessMessage, updateSuccessMessages } from '../../lib/labels'

export function UnitsPage({ module, canManage, lookups }: { module: ModuleDef; canManage: boolean; lookups: LookupData }) {
  const [condominiumId, setCondominiumId] = useState('')
  const endpointPath = condominiumId ? `/api/Units/condominium/${encodeURIComponent(condominiumId)}` : ''
  return (
    <FilteredPage
      module={{ ...module, listEndpoint: endpointPath || undefined }}
      canManage={canManage}
      lookups={lookups}
      filters={
        <div className="filter-row">
          <select value={condominiumId} onChange={event => setCondominiumId(event.target.value)}>
            <option value="">Selecione o condomínio</option>
            {lookups.condominiums.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </div>
      }
    />
  )
}

export const petFilters = [
  { label: 'Por condomínio', lookupKey: 'condominiums' as LookupKey, build: (value: string) => `/api/Pets/condominium/${encodeURIComponent(value)}` },
  { label: 'Por unidade', lookupKey: 'units' as LookupKey, build: (value: string) => `/api/Pets/unit/${encodeURIComponent(value)}` },
  { label: 'Por tutor', lookupKey: 'users' as LookupKey, build: (value: string) => `/api/Pets/owner/${encodeURIComponent(value)}` },
]

export const occurrenceFilters = [
  { label: 'Por condomínio', lookupKey: 'condominiums' as LookupKey, build: (value: string) => `/api/Occurrences/condominium/${encodeURIComponent(value)}` },
  { label: 'Por usuário', lookupKey: 'users' as LookupKey, build: (value: string) => `/api/Occurrences/user/${encodeURIComponent(value)}` },
]

// A API só lista cobranças por morador (GET /api/Bills/user/{userId}).
export const billFilters = [
  { label: 'Por morador', lookupKey: 'users' as LookupKey, build: (value: string) => `/api/Bills/user/${encodeURIComponent(value)}` },
]

export function FilteredPage({
  module,
  filters,
  canManage,
  lookups,
}: {
  module: ModuleDef
  filters: ReactNode | { label: string; lookupKey: LookupKey; build: (value: string) => string }[]
  canManage: boolean
  lookups: LookupData
}) {
  const [selected, setSelected] = useState(0)
  const [filterValue, setFilterValue] = useState('')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [message, setMessage] = useState('')
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null)
  const formFields = withLookupOptions(module.fields, lookups)
  const editFields = withLookupOptions(module.editFields ?? module.fields, lookups)

  const selectedFilter = Array.isArray(filters) ? filters[selected] : undefined
  const selectedOptions = selectedFilter ? lookups[selectedFilter.lookupKey] : []
  const filterNode = Array.isArray(filters) ? (
    <div className="filter-row">
      <select value={selected} onChange={event => setSelected(Number(event.target.value))}>
        {filters.map((filter, index) => <option key={filter.label} value={index}>{filter.label}</option>)}
      </select>
      <select value={filterValue} onChange={event => setFilterValue(event.target.value)}>
        <option value="">Selecione</option>
        {selectedOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
      <button type="button" onClick={loadFiltered}>Consultar</button>
    </div>
  ) : filters

  async function loadFiltered() {
    const endpointPath = module.listEndpoint || (Array.isArray(filters) && filterValue ? filters[selected].build(filterValue) : '')
    if (!endpointPath) return
    try {
      const data = await requestJson<Record<string, unknown>[]>(endpointPath)
      setRows(Array.isArray(data) ? data : [])
      setMessage('')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao consultar.')
    }
  }

  useEffect(() => {
    if (module.listEndpoint) loadFiltered()
  }, [module.listEndpoint])

  async function create(payload: Record<string, unknown>) {
    if (!module.createEndpoint) return
    await requestJson(module.createEndpoint, {
      method: 'POST',
      headers: module.createHeaders?.(payload),
      body: JSON.stringify(payload),
    })
    await loadFiltered()
    setMessage(createSuccessMessages[module.id] ?? 'Registro criado com sucesso.')
  }

  async function update(payload: Record<string, unknown>) {
    const id = editingRow ? normalizeId(editingRow) : ''
    if (!id || !module.createEndpoint) return
    await requestJson(`${module.createEndpoint}/${id}`, {
      method: 'PUT',
      headers: module.createHeaders?.(payload),
      body: JSON.stringify(payload),
    })
    setEditingRow(null)
    await loadFiltered()
    setMessage(updateSuccessMessages[module.id] ?? 'Registro atualizado com sucesso.')
  }

  async function remove(row: Record<string, unknown>) {
    const id = normalizeId(row)
    const deleteEndpoint = module.createEndpoint || module.listEndpoint
    if (!id || !deleteEndpoint) {
      setMessage('Não foi possível identificar o registro para remoção.')
      return
    }
    if (!window.confirm(deleteConfirmationMessage(module.id))) return
    try {
      await requestJson(`${deleteEndpoint}/${id}`, { method: 'DELETE' })
      await loadFiltered()
      setMessage(deleteSuccessMessages[module.id] ?? 'Registro removido com sucesso.')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao remover.')
    }
  }

  return (
    <div className="panel-grid">
      {canManage && formFields && (
        <JsonForm
          title="Novo registro"
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
        <h2>Consulta</h2>
        {filterNode}
        {message && <div className={isSuccessMessage(message) ? 'notice' : 'notice error'}>{message}</div>}
        <DataTable
          rows={rows}
          columns={module.columns}
          lookups={lookups}
          onEdit={canManage && editFields ? row => setEditingRow(row) : undefined}
          onDelete={canManage && module.createEndpoint && !module.noDelete ? remove : undefined}
        />
      </section>
    </div>
  )
}
