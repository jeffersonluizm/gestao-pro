import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { requestJson } from '../api/http'
import { useAuth } from '../auth/AuthContext'
import { LookupData, Row, SelectOption } from '../domain/types'
import { optionFromRow } from '../lib/format'
import { emptyLookups } from './modules'

// Listas de apoio dos formulários e tabelas (nomes de moradores, áreas, unidades...). Carregadas uma vez
// por sessão; quem altera uma delas (ex.: assembleias) pede refresh.
interface LookupsContextValue {
  lookups: LookupData
  refresh: () => Promise<void>
}

const LookupsContext = createContext<LookupsContextValue>({ lookups: emptyLookups, refresh: async () => undefined })

async function readList(path: string): Promise<Row[]> {
  try {
    const data = await requestJson<Row[]>(path)
    return Array.isArray(data) ? data : []
  } catch {
    return []
  }
}

function toOptions(rows: Row[], fallback: string): SelectOption[] {
  return rows.map((row, index) => optionFromRow(row, `${fallback} ${index + 1}`)).filter(Boolean) as SelectOption[]
}

export function LookupsProvider({ children }: { children: ReactNode }) {
  const { session } = useAuth()
  const [lookups, setLookups] = useState<LookupData>(emptyLookups)

  const refresh = useCallback(async () => {
    if (!session.token) {
      setLookups(emptyLookups)
      return
    }
    const condominiumId = encodeURIComponent(session.condominiumId)
    const [condominiums, users, commonAreas, assemblies, units, visitors, deliveries] = await Promise.all([
      readList('/api/Condominiums'),
      readList('/api/Users'),
      readList('/api/CommonAreas'),
      readList('/api/assembly'),
      session.condominiumId ? readList(`/api/Units/condominium/${condominiumId}`) : Promise.resolve([]),
      readList('/api/Visitor'),
      readList('/api/Delivery'),
    ])
    setLookups({
      condominiums: toOptions(condominiums, 'Condomínio'),
      users: toOptions(users, 'Usuário'),
      units: toOptions(units, 'Unidade'),
      commonAreas: toOptions(commonAreas, 'Área comum'),
      assemblies: toOptions(assemblies, 'Assembleia'),
      visitors: toOptions(visitors, 'Visitante'),
      deliveries: toOptions(deliveries, 'Encomenda'),
    })
  }, [session.token, session.condominiumId])

  useEffect(() => { refresh() }, [refresh])

  const value = useMemo(() => ({ lookups, refresh }), [lookups, refresh])
  return <LookupsContext.Provider value={value}>{children}</LookupsContext.Provider>
}

export function useLookups() {
  return useContext(LookupsContext)
}
