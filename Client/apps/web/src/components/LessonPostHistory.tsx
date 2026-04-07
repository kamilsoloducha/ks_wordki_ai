import { useEffect, useMemo, useState } from 'react'
import type { LessonFlashcardAnswerRecord } from '../lib/lessonTypes'
import './LessonPostHistory.css'

const DEFAULT_PAGE_SIZE = 10

export type LessonPostHistoryProps = {
  entries: readonly LessonFlashcardAnswerRecord[]
  /** Liczba wierszy na stronę (domyślnie 10). */
  pageSize?: number
}

export function LessonPostHistory({ entries, pageSize = DEFAULT_PAGE_SIZE }: LessonPostHistoryProps) {
  const [page, setPage] = useState(1)

  const total = entries.length
  const totalPages = total === 0 ? 1 : Math.ceil(total / pageSize)

  useEffect(() => {
    setPage(1)
  }, [entries])

  useEffect(() => {
    setPage((p) => Math.min(Math.max(1, p), totalPages))
  }, [totalPages])

  const sliceStart = (page - 1) * pageSize
  const rows = useMemo(
    () => entries.slice(sliceStart, sliceStart + pageSize),
    [entries, sliceStart, pageSize],
  )

  const canPrev = page > 1
  const canNext = page < totalPages

  return (
    <section className="lesson-post-history" aria-labelledby="lesson-post-history-heading">
      <h2 id="lesson-post-history-heading" className="lesson-post-history__title">
        Historia lekcji
      </h2>
      <p className="lesson-post-history__lead">
        Wszystkie oceny w kolejności — przy „Nie znałem” ta sama karta mogła wrócić na koniec kolejki,
        więc jedno słowo może wystąpić wielokrotnie.
      </p>

      {total > 0 && (
        <p className="lesson-post-history__meta" aria-live="polite">
          Łącznie: {total}
          {totalPages > 1 ? ` · strona ${page} z ${totalPages}` : ''}
        </p>
      )}

      <div className="lesson-post-history__scroll" tabIndex={0}>
        <table className="lesson-post-history__table">
          <thead>
            <tr>
              <th scope="col">#</th>
              <th scope="col">Pytanie</th>
              <th scope="col">Odpowiedź</th>
              <th scope="col">Twój wybór</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) => {
              const globalIndex = sliceStart + i + 1
              return (
                <tr key={`${sliceStart + i}-${row.questionLabel}`}>
                  <td className="lesson-post-history__num">{globalIndex}</td>
                  <td className="lesson-post-history__q">{row.questionLabel}</td>
                  <td className="lesson-post-history__a">{row.answerLabel}</td>
                  <td className="lesson-post-history__choice">
                    <span
                      className={
                        row.knew
                          ? 'lesson-post-history__badge lesson-post-history__badge--knew'
                          : 'lesson-post-history__badge lesson-post-history__badge--dont'
                      }
                    >
                      {row.knew ? 'Znałem' : 'Nie znałem'}
                    </span>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {total === 0 && (
        <p className="lesson-post-history__empty" role="status">
          Brak zapisanych ocen.
        </p>
      )}

      {totalPages > 1 && (
        <nav className="lesson-post-history__pager" aria-label="Paginacja historii lekcji">
          <button
            type="button"
            className="lesson-post-history__page-btn"
            disabled={!canPrev}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            Poprzednia
          </button>
          <span className="lesson-post-history__page-indicator" aria-hidden>
            {page} / {totalPages}
          </span>
          <button
            type="button"
            className="lesson-post-history__page-btn"
            disabled={!canNext}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          >
            Następna
          </button>
        </nav>
      )}
    </section>
  )
}
