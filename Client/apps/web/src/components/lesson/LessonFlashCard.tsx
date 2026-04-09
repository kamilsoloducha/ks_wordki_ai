import { useEffect, useState } from 'react'
import type { LessonQueueItem } from '../../lib/lessonCardSides'
import { LessonCardHeader } from './LessonCardHeader'
import { LessonGradeButtons } from './LessonGradeButtons'

export type LessonFlashCardProps = {
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

export function LessonFlashCard({
  current,
  indexInSession,
  totalInSession,
  tickBusy,
  tickDone,
  tickError,
  onTick,
  onKnew,
  onDidNotKnow,
}: LessonFlashCardProps) {
  const [answerRevealed, setAnswerRevealed] = useState(false)

  useEffect(() => {
    setAnswerRevealed(false)
  }, [current?.questionResultId])

  useEffect(() => {
    if (!current) {
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

      if (!answerRevealed) {
        if (e.key === 'Enter') {
          e.preventDefault()
          setAnswerRevealed(true)
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
  }, [answerRevealed, current, onDidNotKnow, onKnew])

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
      <div className="lesson-flash__card" role="region" aria-label="Karta">
        <div className="lesson-flash__block lesson-flash__block--question">
          <p className="lesson-flash__label">Pytanie</p>
          <p className="lesson-flash__text">{current.question.label}</p>
          {current.question.example.trim() !== '' && (
            <p className="lesson-flash__example">{current.question.example}</p>
          )}
        </div>

        <div
          className={`lesson-flash__block lesson-flash__block--answer${answerRevealed ? '' : ' lesson-flash__block--answer-pending'}`}
        >
          <p className="lesson-flash__label">Odpowiedź</p>
          {answerRevealed ? (
            <>
              <p className="lesson-flash__text">{current.answer.label}</p>
              {current.answer.example.trim() !== '' && (
                <p className="lesson-flash__example">{current.answer.example}</p>
              )}
            </>
          ) : (
            <p className="lesson-flash__answer-placeholder">Ukryta</p>
          )}
        </div>

        {!answerRevealed && (
          <LessonGradeButtons revealAction={{ onClick: () => setAnswerRevealed(true) }} />
        )}

        {answerRevealed && (
          <LessonGradeButtons
            ariaLabel="Oceń, czy znałeś odpowiedź. Skróty: strzałka w lewo — nie znałem, w prawo — znałem."
            negativeLabel="Nie znałem"
            positiveLabel="Znałem"
            onNegative={onDidNotKnow}
            onPositive={onKnew}
          />
        )}
      </div>
    </div>
  )
}
