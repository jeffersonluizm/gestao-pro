import { LookupData } from '../../domain/types'
import { formatCell, formatValue, normalizeId } from '../../lib/format'

export function ReservationCalendar({
  rows,
  lookups,
  currentMonth,
  onMonthChange,
}: {
  rows: Record<string, unknown>[]
  lookups: LookupData
  currentMonth: Date
  onMonthChange: (month: Date) => void
}) {
  const monthLabel = currentMonth.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  const firstDay = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1)
  const startOffset = firstDay.getDay()
  const daysInMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 0).getDate()
  const cells = Array.from({ length: startOffset + daysInMonth }, (_, index) =>
    index < startOffset ? null : index - startOffset + 1
  )

  function moveMonth(offset: number) {
    onMonthChange(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + offset, 1))
  }

  function reservationsFor(day: number) {
    const dateKey = `${currentMonth.getFullYear()}-${String(currentMonth.getMonth() + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    return rows.filter(row => String(row.date ?? '').slice(0, 10) === dateKey)
  }

  return (
    <div className="calendar-panel">
      <div className="calendar-header">
        <button type="button" className="secondary-button" onClick={() => moveMonth(-1)}>Anterior</button>
        <strong>{monthLabel}</strong>
        <button type="button" className="secondary-button" onClick={() => moveMonth(1)}>Próximo</button>
      </div>
      <div className="calendar-weekdays">
        {['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'].map(day => <span key={day}>{day}</span>)}
      </div>
      <div className="calendar-grid">
        {cells.map((day, index) => (
          <div className={day ? 'calendar-day' : 'calendar-day muted-day'} key={`${day ?? 'empty'}-${index}`}>
            {day && (
              <>
                <span className="calendar-date">{day}</span>
                <div className="calendar-events">
                  {reservationsFor(day).map((reservation, eventIndex) => {
                    const area = formatCell('commonAreaId', reservation.commonAreaId, lookups)
                    const status = formatValue(reservation.status)
                    return (
                      <div className="calendar-event" key={`${normalizeId(reservation)}-${eventIndex}`}>
                        <strong>{area}</strong>
                        <span>{reservation.startTime as string} - {reservation.endTime as string}</span>
                        <em>{status}</em>
                      </div>
                    )
                  })}
                </div>
              </>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
