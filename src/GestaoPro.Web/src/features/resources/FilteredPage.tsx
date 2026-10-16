import { useEffect, useState } from 'react'
import { JsonForm } from '../../components/form/ResourceForm'
import { DataTable } from '../../components/ui/DataTable'
import { ErrorState, LoadingState } from '../../components/ui/States'
import { AppSession, LookupData, LookupKey, ModuleDef, Row } from '../../domain/types'
import { normalizeId } from '../../lib/format'
import { withLookupOptions, withoutSessionFields } from '../../lib/forms'
import { useResourceCrud } from './useResourceCrud'

export interface ListFilter {
  label: string
  lookupKey: LookupKey
  build: (value: string) => string
}

export const petFilters: ListFilter[] = [
  { label: 'Por unidade', lookupKey: 'units', build: value => `/api/Pets/unit/${encodeURIComponent(value)}` },
  { label: 'Por tutor', lookupKey: 'users', build: value => `/api/Pets/owner/${encodeURIComponent(value)}` },
]

export const occurrenceFilters: ListFilter[] = [
  { label: 'Por morador', lookupKey: 'users', build: value => `/api/Occurrences/user/${encodeURIComponent(value)}` },
]

// A API só lista cobranças por morador (GET /api/Bills/user/{userId}).
export const billFilters: ListFilter[] = [
  { label: 'Por morador', lookupKey: 'users', build: value => `/api/Bills/user/${encodeURIComponent(value)}` },
]

// Lista que carrega direto do endpoint da sessão (quando há) e, para a gestão, permite filtrar.
export function FilteredPage({
  module,
  filters,
  canManage,
  session,
  lookups,
}: {
  module: ModuleDef
  filters: ListFilter[] | null
  canManage: boolean
  session: AppSession
  lookups: LookupData
}) {
  const [selected, setSelected] = useState(0)
  const [filterValue, setFilterValue] = useState('')
  const [currentPath, setCurrentPath] = useState(module.listEndpoint)
  const [editingRow, setEditingRow] = useState<Row | null>(null)
  const crud = useResourceCrud(module, session, { list: currentPath, write: module.createEndpoint })
  const residentOccurrence = module.id === 'occurrences' && !canManage
  const formFields = withLookupOptions(
    withoutSessionFields(residentOccurrence ? module.fields?.filter(field => !['userId', 'status'].includes(field.name)) : module.fields),
    lookups,
  )
  const editFields = withLookupOptions(withoutSessionFields(module.editFields ?? module.fields), lookups)
  const canCreate = Boolean(formFields?.length && module.createEndpoint && (canManage || residentOccurrence))
  const selectedFilter = filters?.[selected]
  const { load } = crud

  useEffect(() => { load() }, [load])

  async function consult() {
    if (!selectedFilter || !filterValue) return
    const path = selectedFilter.build(filterValue)
    // Caminho novo recarrega pelo efeito; o mesmo caminho recarrega aqui (botão Consultar de novo).
    if (path === currentPath) await load(path)
    else setCurrentPath(path)
  }

  async function create(payload: Row) {
    await crud.create(payload, residentOccurrence ? { userId: session.userId } : {})
    await load(currentPath)
  }

  async function update(payload: Row) {
    if (!editingRow) return
    await crud.update(editingRow, payload)
    setEditingRow(null)
    await load(currentPath)
  }

  async function remove(row: Row) {
    if (await crud.remove(row)) await load(currentPath)
  }

  return (
    <div className={canCreate ? 'panel-grid' : 'panel-grid read-only'}>
      {canCreate && formFields && (
        <JsonForm title={residentOccurrence ? 'Nova ocorrência' : 'Novo registro'} fields={formFields} onSubmit={create} successMessage="" />
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
        <h2>{residentOccurrence ? 'Minhas ocorrências' : 'Consulta'}</h2>
        {filters && canManage && (
          <div className="filter-row">
            <label className="field">
              <span className="field-label">Filtrar</span>
              <select value={selected} onChange={event => { setSelected(Number(event.target.value)); setFilterValue('') }}>
                {filters.map((filter, index) => <option key={filter.label} value={index}>{filter.label}</option>)}
              </select>
            </label>
            <label className="field">
              <span className="field-label">Valor</span>
              <select value={filterValue} onChange={event => setFilterValue(event.target.value)}>
                <option value="">Selecione</option>
                {(selectedFilter ? lookups[selectedFilter.lookupKey] : []).map(option => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>
            <button type="button" onClick={consult} disabled={!filterValue} style={{ alignSelf: 'end' }}>Consultar</button>
          </div>
        )}
        {crud.error && <ErrorState message={crud.error} onRetry={() => load(currentPath)} />}
        {crud.loading && !crud.rows.length ? <LoadingState /> : (
          <DataTable
            rows={crud.rows}
            columns={module.columns}
            lookups={lookups}
            caption={module.title}
            emptyTitle={currentPath ? 'Nenhum registro encontrado.' : 'Escolha um filtro e clique em Consultar.'}
            onEdit={canManage && editFields ? row => setEditingRow(row) : undefined}
            onDelete={canManage && module.createEndpoint && !module.noDelete ? remove : undefined}
          />
        )}
      </section>
    </div>
  )
}
