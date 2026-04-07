/** Rodzaj lekcji w ustawieniach — w przyszłości można dodać kolejne warianty (np. audio). */
export type LessonMode = 'flashcards' | 'typing'

/** Skąd bierzemy słowa: powtórki (termin do końca dziś UTC) albo strony bez zaplanowanego powtórzenia. */
export type LessonWordSource = 'review' | 'newWords'

/** Kierunek: strona pytania → strona odpowiedzi (typy z grup: front/back). */
export type LessonQuestionDirection = {
  questionSideType: string
  answerSideType: string
}

export type LessonSessionSettings = {
  wordSource: LessonWordSource
  lessonMode: LessonMode
  direction: LessonQuestionDirection
  wordsInLesson: number
}

/** Stan przekazywany przez `navigate('/lesson', { state })`. */
export type LessonLocationState = {
  settings: LessonSessionSettings
}

/** Pojedyncza ocena w sesji fiszek (kolejność = kolejność kliknięć „Znałem” / „Nie znałem”). */
export type LessonFlashcardAnswerRecord = {
  readonly questionLabel: string
  readonly answerLabel: string
  /** `true` — „Znałem”, `false` — „Nie znałem”. */
  readonly knew: boolean
}
