import { useCallback, useEffect, useRef, useState } from 'react'
import type { LessonQueueItem } from '../../lib/lessonCardSides'
import { LessonCardHeader } from './LessonCardHeader'
import { LessonGradeButtons } from './LessonGradeButtons'

export type LessonTypingCardProps = {
  current: LessonQueueItem | null
  indexInSession: number
  totalInSession: number
  tickBusy: boolean
  tickDone: boolean
  tickError: string | null
  onTick: () => void
  onKnew: () => void | Promise<void>
  onDidNotKnow: () => void | Promise<void>
}

export function LessonTypingCard({
  current,
  indexInSession,
  totalInSession,
  tickBusy,
  tickDone,
  tickError,
  onTick,
  onKnew,
  onDidNotKnow,
}: LessonTypingCardProps) {
  const [typingDraft, setTypingDraft] = useState('')
  const [typingChecked, setTypingChecked] = useState(false)
  const [typingExactMatch, setTypingExactMatch] = useState<boolean | null>(null)
  const typingInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    setTypingDraft('')
    setTypingChecked(false)
    setTypingExactMatch(null)
  }, [current?.questionResultId])

  const submitTypingAnswer = useCallback(() => {
    if (typingChecked || !current) return
    const expected = current.answer.label.trim()
    const got = typingDraft.trim()
    setTypingExactMatch(got === expected)
    setTypingChecked(true)
  }, [current, typingChecked, typingDraft])

  useEffect(() => {
    if (!current || typingChecked) {
      return
    }
    const id = requestAnimationFrame(() => typingInputRef.current?.focus())
    return () => cancelAnimationFrame(id)
  }, [current, typingChecked])

  useEffect(() => {
    if (!typingChecked || !current) {
      return
    }

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLElement) {
        const tag = e.target.tagName
        if (
          tag === 'INPUT' ||
          tag === 'TEXTAREA' ||
          tag === 'SELECT' ||
          e.target.isContentEditable
        ) {
          return
        }
      }

      if (e.key === 'Enter') {
        e.preventDefault()
        if (typingExactMatch === true) {
          void onKnew()
        } else if (typingExactMatch === false) {
          void onDidNotKnow()
        }
        return
      }

      if (e.key === 'ArrowLeft') {
        e.preventDefault()
        void onDidNotKnow()
      } else if (e.key === 'ArrowRight') {
        e.preventDefault()
        void onKnew()
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [current, onDidNotKnow, onKnew, typingChecked, typingExactMatch])

  if (!current) {
    return null
  }

  return (
    <div className="lesson-flash">
      <LessonCardHeader
        indexInSession={indexInSession}
        totalInSession={totalInSession}
        tickBusy={tickBusy}
        tickDone={tickDone}
        tickError={tickError}
        questionResultId={current.questionResultId}
        onTick={onTick}
      />
      <div className="lesson-flash__card" role="region" aria-label="Karta — tryb wpisywania">
        <div className="lesson-flash__block lesson-flash__block--question">
          <p className="lesson-flash__label">Pytanie</p>
          <p className="lesson-flash__text">{current.question.label}</p>
          {current.question.example.trim() !== '' && (
            <p className="lesson-flash__example">{current.question.example}</p>
          )}
        </div>

        {!typingChecked && (
          <>
            <form
              id="lesson-typing-answer"
              className="lesson-flash__type-form"
              onSubmit={(e) => {
                e.preventDefault()
                submitTypingAnswer()
              }}
            >
              <label className="lesson-flash__type-label" htmlFor="lesson-type-answer">
                Twoja odpowiedź
              </label>
              <input
                id="lesson-type-answer"
                ref={typingInputRef}
                type="text"
                className="lesson-flash__type-input"
                value={typingDraft}
                onChange={(e) => setTypingDraft(e.target.value)}
                autoComplete="off"
                spellCheck={false}
                disabled={typingChecked}
              />
            </form>
            <LessonGradeButtons
              ariaLabel="Sprawdź wpisaną odpowiedź. Możesz też nacisnąć Enter w polu tekstowym."
              checkAction={{ formId: 'lesson-typing-answer', label: 'Sprawdź' }}
            />
          </>
        )}

        {typingChecked && typingExactMatch !== null && (
          <>
            <div
              className={`lesson-flash__block lesson-flash__type-verdict${typingExactMatch ? ' lesson-flash__type-verdict--ok' : ' lesson-flash__type-verdict--bad'}`}
              role="status"
            >
              <p className="lesson-flash__type-verdict-text">
                {typingExactMatch
                  ? 'Zgodne z oczekiwaną odpowiedzią.'
                  : 'Niezgodne z oczekiwaną odpowiedzią.'}
              </p>
              <div className="lesson-flash__type-answer-box">
                <p className="lesson-flash__label">Odpowiedź</p>
                <p className="lesson-flash__text">{current.answer.label}</p>
                {current.answer.example.trim() !== '' && (
                  <p className="lesson-flash__example">{current.answer.example}</p>
                )}
              </div>
            </div>
            <LessonGradeButtons
              ariaLabel="Jak oceniasz swoją znajomość? Enter — sugerowany wybór, strzałka w lewo — nie wiem, w prawo — wiem."
              negativeLabel="Nie wiem"
              positiveLabel="Wiem"
              onNegative={onDidNotKnow}
              onPositive={onKnew}
              suggested={typingExactMatch ? 'positive' : 'negative'}
            />
          </>
        )}
      </div>
    </div>
  )
}
