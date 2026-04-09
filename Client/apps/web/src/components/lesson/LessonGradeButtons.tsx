export type LessonGradeButtonsProps = {
  /** Opis grupy przycisków oceny / sprawdzenia (tylko gdy jest siatka z „Sprawdź” lub oceną). */
  ariaLabel?: string
  /** Fiszki — odsłonięcie odpowiedzi przed oceną. */
  revealAction?: {
    onClick: () => void
  }
  /** Powiązanie z `<form id="...">` — `type="submit"` poza formularzem. */
  checkAction?: {
    formId: string
    label?: string
    disabled?: boolean
  }
  negativeLabel?: string
  positiveLabel?: string
  onNegative?: () => void
  onPositive?: () => void
  /** Domyślny Enter (tryb wpisywania) — podświetla odpowiedni przycisk. */
  suggested?: 'negative' | 'positive' | null
}

function negativeTitle(suggested: LessonGradeButtonsProps['suggested']): string {
  return suggested === 'negative'
    ? 'Skrót: Enter lub strzałka w lewo (←)'
    : 'Skrót: strzałka w lewo (←)'
}

function positiveTitle(suggested: LessonGradeButtonsProps['suggested']): string {
  return suggested === 'positive'
    ? 'Skrót: Enter lub strzałka w prawo (→)'
    : 'Skrót: strzałka w prawo (→)'
}

export function LessonGradeButtons({
  ariaLabel,
  revealAction,
  checkAction,
  negativeLabel,
  positiveLabel,
  onNegative,
  onPositive,
  suggested = null,
}: LessonGradeButtonsProps) {
  const hasGrade =
    negativeLabel != null &&
    positiveLabel != null &&
    onNegative != null &&
    onPositive != null

  const hasGrid = Boolean(checkAction || hasGrade)

  if (!revealAction && !hasGrid) {
    return null
  }

  return (
    <div className="lesson-flash__controls">
      {revealAction && (
        <button
          type="button"
          className="lesson-flash__reveal"
          title="Skrót: Enter"
          onClick={revealAction.onClick}
        >
          Pokaż odpowiedź
          <span className="lesson-flash__reveal-hint">Enter</span>
        </button>
      )}
      {hasGrid && (
        <div
          className="lesson-flash__grade"
          role="group"
          {...(ariaLabel ? { 'aria-label': ariaLabel } : {})}
        >
          {checkAction && (
            <button
              type="submit"
              form={checkAction.formId}
              className="lesson-flash__type-submit lesson-flash__grade-check"
              disabled={checkAction.disabled}
              title="Zatwierdź wpis (Enter w polu odpowiedzi)"
            >
              {checkAction.label ?? 'Sprawdź'}
              <span className="lesson-flash__reveal-hint">Enter</span>
            </button>
          )}
          {hasGrade && (
            <>
              <button
                type="button"
                className={`lesson-flash__btn lesson-flash__btn--dont${suggested === 'negative' ? ' lesson-flash__btn--suggested' : ''}`}
                title={negativeTitle(suggested)}
                onClick={onNegative}
              >
                {negativeLabel}
              </button>
              <button
                type="button"
                className={`lesson-flash__btn lesson-flash__btn--know${suggested === 'positive' ? ' lesson-flash__btn--suggested' : ''}`}
                title={positiveTitle(suggested)}
                onClick={onPositive}
              >
                {positiveLabel}
              </button>
            </>
          )}
        </div>
      )}
    </div>
  )
}
