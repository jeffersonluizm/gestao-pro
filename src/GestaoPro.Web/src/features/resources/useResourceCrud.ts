import { useCallback, useState } from 'react'
import { requestJson } from '../../api/http'
import { useConfirm } from '../../components/ui/ConfirmDialog'
import { useToast } from '../../components/ui/Toast'
import { AppSession, ModuleDef, Row } from '../../domain/types'
import { normalizeId } from '../../lib/format'
import { withSessionCondominium } from '../../lib/forms'
import { createSuccessMessages, deleteConfirmationMessage, deleteSuccessMessages, updateSuccessMessages } from '../../lib/labels'

// Ciclo de vida de uma tela de cadastro guiada por configuração: carregar, criar, editar e remover,
// com o condomínio da sessão no corpo, confirmação própria para remover e aviso (toast) de resultado.
export function useResourceCrud(module: ModuleDef, session: AppSession, endpoints: { list?: string; write?: string }) {
  const toast = useToast()
  const confirm = useConfirm()
  const [rows, setRows] = useState<Row[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const load = useCallback(async (path = endpoints.list) => {
    if (!path) return
    setLoading(true)
    setError('')
    try {
      const data = await requestJson<Row[]>(path)
      setRows(Array.isArray(data) ? data : [])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar.')
    } finally {
      setLoading(false)
    }
  }, [endpoints.list])

  const create = useCallback(async (payload: Row, extra: Row = {}) => {
    if (!module.createEndpoint) return
    const body = withSessionCondominium(module, { ...payload, ...extra }, session)
    const created = await requestJson<Row>(module.createEndpoint, {
      method: 'POST',
      headers: module.createHeaders?.(body),
      body: JSON.stringify(body),
    })
    toast.success(module.id === 'users' && created?.temporaryPassword
      ? `Usuário criado. Senha temporária (anote, não aparece de novo): ${String(created.temporaryPassword)}`
      : createSuccessMessages[module.id] ?? 'Registro criado com sucesso.')
    return created
  }, [module, session, toast])

  const update = useCallback(async (row: Row, payload: Row, extra: Row = {}) => {
    const id = normalizeId(row)
    const path = endpoints.write ?? module.listEndpoint ?? module.createEndpoint
    if (!id || !path) return
    const body = withSessionCondominium(module, { ...payload, ...extra }, session)
    await requestJson(`${path}/${id}`, { method: 'PUT', headers: module.createHeaders?.(body), body: JSON.stringify(body) })
    toast.success(updateSuccessMessages[module.id] ?? 'Registro atualizado com sucesso.')
  }, [endpoints.write, module, session, toast])

  // Devolve true quando removeu (para a tela recarregar).
  const remove = useCallback(async (row: Row) => {
    const id = normalizeId(row)
    const path = endpoints.write ?? module.listEndpoint ?? module.createEndpoint
    if (!id || !path) {
      toast.error('Não foi possível identificar o registro para remoção.')
      return false
    }
    const ok = await confirm({
      title: module.id === 'users' ? 'Desativar usuário' : 'Remover registro',
      message: deleteConfirmationMessage(module.id),
      confirmLabel: module.id === 'users' ? 'Desativar' : 'Remover',
      destructive: true,
    })
    if (!ok) return false
    try {
      await requestJson(`${path}/${id}`, { method: 'DELETE' })
      toast.success(deleteSuccessMessages[module.id] ?? 'Registro removido com sucesso.')
      return true
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Não foi possível remover.')
      return false
    }
  }, [confirm, endpoints.write, module, toast])

  return { rows, setRows, loading, error, load, create, update, remove }
}
