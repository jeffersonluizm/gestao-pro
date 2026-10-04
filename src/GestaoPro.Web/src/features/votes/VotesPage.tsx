import { FormEvent, useState } from 'react'
import { requestJson } from '../../api/http'
import { DataTable } from '../../components/ui/DataTable'
import { PanelHeader } from '../../components/ui/PanelHeader'
import { AppSession, LookupData, ModuleDef } from '../../domain/types'
import { formatDateTime } from '../../lib/format'
import { formatOptionLabel } from '../../lib/labels'

export function VotesPage({ module, canManage, session, lookups }: { module: ModuleDef; canManage: boolean; session: AppSession; lookups: LookupData }) {
  const [assemblyId, setAssemblyId] = useState('')
  const [topicIndex, setTopicIndex] = useState('')
  const [vote, setVote] = useState('')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [assemblyDetails, setAssemblyDetails] = useState<Record<string, unknown> | null>(null)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const selectedAssembly = lookups.assemblies.find(option => option.value === assemblyId)
  const topics = Array.isArray(assemblyDetails?.topics) ? assemblyDetails.topics as Record<string, unknown>[] : []
  const selectedTopic = topicIndex === '' ? undefined : topics[Number(topicIndex)]
  const voteOptions = Array.isArray(selectedTopic?.voteOptions)
    ? selectedTopic.voteOptions.map(option => String(option))
    : ['sim', 'não']
  const topicRows = topicIndex === '' ? [] : rows.filter(row => Number(row.topicIndex) === Number(topicIndex))
  const historyRows = rows.filter(row => !row.summaryOnly)
  const voteSummary = topicRows[0] ? {
    yesVotes: Number(topicRows[0].yesVotes ?? 0),
    noVotes: Number(topicRows[0].noVotes ?? 0),
    winner: String(topicRows[0].winner ?? 'Empate'),
    voteFinished: Boolean(topicRows[0].voteFinished ?? false),
  } : null

  async function loadVotes(nextAssemblyId = assemblyId) {
    if (!nextAssemblyId) {
      setAssemblyDetails(null)
      setRows([])
      return
    }
    setMessage('')
    try {
      const [details, data] = await Promise.all([
        requestJson<Record<string, unknown>>(`/api/assembly/${encodeURIComponent(nextAssemblyId)}`),
        requestJson<Record<string, unknown>[]>(`/api/vote/assembly/${encodeURIComponent(nextAssemblyId)}`),
      ])
      setAssemblyDetails(details)
      const safeRows = Array.isArray(data) ? data : []
      setRows(safeRows)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao carregar a votação.')
    }
  }

  async function createVote(event: FormEvent) {
    event.preventDefault()
    if (!assemblyId) return setMessage('Selecione a assembleia.')
    if (topicIndex === '') return setMessage('Selecione a pauta.')
    if (!session.userId) return setMessage('Usuário não autenticado.')
    if (!vote) return setMessage('Escolha uma opção de voto.')
    if (rows.some(row => !row.summaryOnly && String(row.userId ?? '') === session.userId && Number(row.topicIndex) === Number(topicIndex))) {
      return setMessage('O usuário já votou nesta pauta.')
    }

    setBusy(true)
    setMessage('')
    try {
      await requestJson('/api/vote', {
        method: 'POST',
        body: JSON.stringify({ assemblyId, topicIndex: Number(topicIndex), vote }),
      })
      setMessage('Voto registrado.')
      setVote('')
      await loadVotes()
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro ao registrar voto.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="panel-grid">
      <section className="panel">
        <h2>Novo voto</h2>
        <form className="form-grid" onSubmit={createVote}>
          <label>
            <span>Assembleia *</span>
            <select value={assemblyId} onChange={event => {
              const nextAssemblyId = event.target.value
              setAssemblyId(nextAssemblyId)
              setTopicIndex('')
              setVote('')
              loadVotes(nextAssemblyId)
            }} required>
              <option value="">Selecione</option>
              {lookups.assemblies.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
          </label>
          <label>
            <span>Pauta *</span>
            <select value={topicIndex} onChange={event => { setTopicIndex(event.target.value); setVote('') }} required disabled={!topics.length}>
              <option value="">Selecione</option>
              {topics.map((topic, index) => <option key={index} value={index}>{String(topic.title ?? `Pauta ${index + 1}`)}</option>)}
            </select>
          </label>
          <label>
            <span>Voto *</span>
            <select value={vote} onChange={event => setVote(event.target.value)} required disabled={topicIndex === ''}>
              <option value="">Selecione</option>
              {voteOptions.map(option => <option key={option} value={option}>{formatOptionLabel(option)}</option>)}
            </select>
          </label>
          <div className="form-actions span-two">
            <button type="submit" disabled={busy || topicIndex === ''}>{busy ? 'Registrando...' : 'Registrar voto'}</button>
          </div>
        </form>
        {message && <div className={message === 'Voto registrado.' ? 'notice' : 'notice error'}>{message}</div>}
      </section>
      <section className="panel">
        <PanelHeader title="Assembleias" onRefresh={() => loadVotes()} />
        {selectedAssembly && (
          <div className="notice" style={{ marginTop: 8, display: 'grid', gap: 6 }}>
            <div><strong>Assembleia:</strong> {selectedAssembly.label}</div>
            {assemblyDetails?.description ? <div>{String(assemblyDetails.description)}</div> : null}
            <div><strong>Status da votação:</strong> {String(assemblyDetails?.voteStatus ?? 'Em andamento')}</div>
            <div><strong>Período:</strong> {formatDateTime(String(assemblyDetails?.startDate ?? assemblyDetails?.startTime))} até {formatDateTime(String(assemblyDetails?.endDate ?? assemblyDetails?.endTime))}</div>
            {selectedTopic && <div><strong>Pauta:</strong> {String(selectedTopic.title ?? '')}{selectedTopic.desc ? ` - ${String(selectedTopic.desc)}` : ''}</div>}
            {voteSummary?.voteFinished && (
              <div style={{ borderTop: '1px solid #e5e7eb', paddingTop: 6 }}>
                <strong>Resultado final:</strong> Sim: {voteSummary.yesVotes} votos · Não: {voteSummary.noVotes} votos
                <br />
                <strong>Vencedor:</strong> {voteSummary.winner}
              </div>
            )}
          </div>
        )}
        {assemblyDetails && (
          <div className="topic-results-grid">
            {topics.map((topic, index) => {
              const summary = rows.find(row => Number(row.topicIndex) === index)
              const counts = summary?.optionCounts && typeof summary.optionCounts === 'object'
                ? summary.optionCounts as Record<string, number>
                : {}
              return (
                <article className="topic-result-card" key={index}>
                  <strong>{String(topic.title ?? `Pauta ${index + 1}`)}</strong>
                  {Object.entries(counts).length
                    ? Object.entries(counts).map(([option, count]) => <span key={option}>{formatOptionLabel(option)}: {count}</span>)
                    : <span>Nenhum voto registrado.</span>}
                  <span>Total da pauta: {Number(summary?.totalVotes ?? 0)}</span>
                </article>
              )
            })}
          </div>
        )}
        <DataTable
          rows={topicIndex === '' ? historyRows : topicRows.filter(row => !row.summaryOnly)}
          columns={canManage ? module.columns : module.columns?.filter(column => column !== 'userId')}
          lookups={lookups}
        />
      </section>
    </div>
  )
}
