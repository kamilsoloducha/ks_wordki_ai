export type LessonCardHeaderProps = {
  indexInSession: number
  totalInSession: number
  tickBusy: boolean
  tickDone: boolean
  tickError: string | null
  questionResultId: number | undefined
  onTick: () => void
}

export function LessonCardHeader({
  indexInSession,
  totalInSession,
  tickBusy,
  tickDone,
  tickError,
  questionResultId,
  onTick,
}: LessonCardHeaderProps) {
  return (
    <>
      <div className="lesson-flash__top">
        <p className="lesson-flash__progress" aria-live="polite">
          Karta {indexInSession} z {totalInSession}
        </p>
        <button
          type="button"
          className="lesson-flash__tick"
          disabled={tickBusy || tickDone || typeof questionResultId !== 'number'}
          title="Oznacz stronę pytania (tick) w bazie"
          onClick={onTick}
        >
          {tickBusy ? '…' : tickDone ? '✓ Tick' : 'Tick'}
        </button>
      </div>
      {tickError && (
        <p className="lesson-flash__tick-msg lesson-flash__tick-msg--error" role="alert">
          {tickError}
        </p>
      )}
    </>
  )
}
