import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { WordkiApiError, type GroupCard, type UserCardGroup } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import { useWordkiBackend } from '../hooks/useWordkiBackend'
import { LessonPostHistory } from '../components/LessonPostHistory'
import {
  buildLessonQueue,
  type LessonQueueItem,
} from '../lib/lessonCardSides'
import type {
  LessonFlashcardAnswerRecord,
  LessonLocationState,
  LessonSessionSettings,
} from '../lib/lessonTypes'
import './LessonPage.css'
import './RegisterPage.css'

function isLessonState(x: unknown): x is LessonLocationState {
  if (x === null || typeof x !== 'object') {
    return false
  }
  const o = x as Record<string, unknown>
  const s = o.settings
  if (s === null || typeof s !== 'object') {
    return false
  }
  const st = s as Record<string, unknown>
  const d = st.direction
  if (d === null || typeof d !== 'object') {
    return false
  }
  const dir = d as Record<string, unknown>
  const ws = st.wordSource
  return (
    (ws === 'review' || ws === 'newWords') &&
    (st.lessonMode === 'flashcards' || st.lessonMode === 'typing') &&
    typeof st.wordsInLesson === 'number' &&
    typeof dir.questionSideType === 'string' &&
    typeof dir.answerSideType === 'string'
  )
}

function groupTypesMap(groups: UserCardGroup[]) {
  const m = new Map<number, { frontSideType: string; backSideType: string }>()
  for (const g of groups) {
    m.set(g.id, { frontSideType: g.frontSideType, backSideType: g.backSideType })
  }
  return m
}

export function LessonPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, user } = useAuth()
  const api = useWordkiBackend()

  const settings: LessonSessionSettings | null = useMemo(() => {
    const st = location.state
    return isLessonState(st) ? st.settings : null
  }, [location.state])

  const [cards, setCards] = useState<GroupCard[] | null>(null)
  const [userGroups, setUserGroups] = useState<UserCardGroup[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [loadingCards, setLoadingCards] = useState(!!settings)
  const [lessonStarted, setLessonStarted] = useState(false)
  const [lessonId, setLessonId] = useState<number | null>(null)
  const [startingLesson, setStartingLesson] = useState(false)
  const [queue, setQueue] = useState<LessonQueueItem[]>([])
  const [totalInSession, setTotalInSession] = useState(0)
  const [answerRevealed, setAnswerRevealed] = useState(false)
  const [answerHistory, setAnswerHistory] = useState<LessonFlashcardAnswerRecord[]>([])
  const [tickBusy, setTickBusy] = useState(false)
  const [tickDone, setTickDone] = useState(false)
  const [tickError, setTickError] = useState<string | null>(null)

  /** Tryb wpisywania: wpis użytkownika i etap po zatwierdzeniu Enter. */
  const [typingDraft, setTypingDraft] = useState('')
  const [typingChecked, setTypingChecked] = useState(false)
  const [typingExactMatch, setTypingExactMatch] = useState<boolean | null>(null)
  const typingInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: '/lesson' } })
    }
  }, [isAuthenticated, navigate])

  useEffect(() => {
    if (!isAuthenticated) {
      return
    }
    if (!settings) {
      navigate('/lesson-settings', { replace: true })
    }
  }, [isAuthenticated, navigate, settings])

  useEffect(() => {
    if (!isAuthenticated || !user?.id || !settings) {
      return
    }

    let cancelled = false
    setLoadingCards(true)
    setLoadError(null)
    setCards(null)
    setUserGroups(null)

    void (async () => {
      try {
        const [list, groups] = await Promise.all([
          api.getDueTodayCards(user.id, {
            direction: settings.direction,
            limit: settings.wordsInLesson,
            wordSource: settings.wordSource,
          }),
          api.getUserCardGroups(user.id),
        ])
        if (cancelled) return
        setCards(list)
        setUserGroups(groups)
      } catch (err) {
        if (cancelled) return
        if (err instanceof WordkiApiError) {
          setLoadError(
            err.errors[0]?.message ??
              `Nie udało się wczytać kart (${err.status ?? 'błąd'}).`,
          )
        } else {
          setLoadError(
            err instanceof Error ? err.message : 'Nie udało się wczytać kart.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingCards(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [api, isAuthenticated, settings, user?.id])

  const currentResultId = queue[0]?.questionResultId

  useEffect(() => {
    setTickDone(false)
    setTickError(null)
  }, [currentResultId])

  useEffect(() => {
    setTypingDraft('')
    setTypingChecked(false)
    setTypingExactMatch(null)
  }, [currentResultId])

  const startLesson = useCallback(async () => {
    if (!settings || !cards?.length || !userGroups || !user?.id) return
    setLoadError(null)
    const map = groupTypesMap(userGroups)
    const q = buildLessonQueue(cards, map, settings.direction)
    if (q.length === 0) {
      setLoadError(
        'Brak kart z przypisanym wynikiem powtórki dla wybranego kierunku — odśwież lub zmień ustawienia lekcji.',
      )
      return
    }
    setStartingLesson(true)
    try {
      const { id } = await api.createLesson(user.id, {
        lessonKind: settings.lessonMode,
        wordCount: q.length,
      })
      setLessonId(id)
      setQueue(q)
      setTotalInSession(q.length)
      setAnswerRevealed(false)
      setAnswerHistory([])
      setLessonStarted(true)
    } catch (err) {
      if (err instanceof WordkiApiError) {
        setLoadError(
          err.errors[0]?.message ??
            `Nie udało się rozpocząć lekcji (${err.status ?? 'błąd'}).`,
        )
      } else {
        setLoadError(
          err instanceof Error ? err.message : 'Nie udało się rozpocząć lekcji.',
        )
      }
    } finally {
      setStartingLesson(false)
    }
  }, [api, cards, settings, user?.id, userGroups])

  const submitTypingAnswer = useCallback(() => {
    if (typingChecked) return
    const item = queue[0]
    if (!item) return
    const expected = item.answer.label.trim()
    const got = typingDraft.trim()
    setTypingExactMatch(got === expected)
    setTypingChecked(true)
  }, [queue, typingChecked, typingDraft])

  useEffect(() => {
    if (
      !lessonStarted ||
      settings?.lessonMode !== 'typing' ||
      !queue[0] ||
      typingChecked
    ) {
      return
    }
    const id = requestAnimationFrame(() => typingInputRef.current?.focus())
    return () => cancelAnimationFrame(id)
  }, [lessonStarted, settings?.lessonMode, typingChecked, currentResultId])

  const onKnew = useCallback(async () => {
    if (!user?.id || lessonId === null) return
    const current = queue[0]
    if (!current) return
    const record: LessonFlashcardAnswerRecord = {
      questionLabel: current.question.label,
      answerLabel: current.answer.label,
      knew: true,
    }
    try {
      await api.addLessonRepetition(user.id, lessonId, {
        questionResultId: current.questionResultId,
        result: true,
      })
    } catch (e) {
      console.error(e)
    }
    setAnswerHistory((prev) => [...prev, record])
    setQueue((prev) => prev.slice(1))
    setAnswerRevealed(false)
  }, [api, user?.id, lessonId, queue])

  const onDidNotKnow = useCallback(async () => {
    if (!user?.id || lessonId === null) return
    const current = queue[0]
    if (!current) return
    const record: LessonFlashcardAnswerRecord = {
      questionLabel: current.question.label,
      answerLabel: current.answer.label,
      knew: false,
    }
    try {
      await api.addLessonRepetition(user.id, lessonId, {
        questionResultId: current.questionResultId,
        result: false,
      })
    } catch (e) {
      console.error(e)
    }
    setAnswerHistory((prev) => [...prev, record])
    setQueue((prev) => {
      if (prev.length === 0) return prev
      const [first, ...rest] = prev
      return [...rest, first]
    })
    setAnswerRevealed(false)
  }, [api, user?.id, lessonId, queue])

  useEffect(() => {
    if (!lessonStarted || settings?.lessonMode !== 'flashcards' || !queue[0]) {
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
  }, [
    answerRevealed,
    lessonStarted,
    onDidNotKnow,
    onKnew,
    queue,
    settings?.lessonMode,
  ])

  useEffect(() => {
    if (!lessonStarted || settings?.lessonMode !== 'typing' || !queue[0] || !typingChecked) {
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
  }, [
    lessonStarted,
    onDidNotKnow,
    onKnew,
    queue,
    settings?.lessonMode,
    typingChecked,
    typingExactMatch,
  ])

  const onTickResult = useCallback(async () => {
    const item = queue[0]
    if (!user?.id || !item?.questionResultId) return
    setTickError(null)
    setTickBusy(true)
    try {
      await api.tickCardResult(user.id, item.questionResultId)
      setTickDone(true)
    } catch (err) {
      if (err instanceof WordkiApiError) {
        setTickError(
          err.errors[0]?.message ?? `Nie udało się zapisać (${err.status ?? 'błąd'}).`,
        )
      } else {
        setTickError(
          err instanceof Error ? err.message : 'Nie udało się oznaczyć strony.',
        )
      }
    } finally {
      setTickBusy(false)
    }
  }, [api, user?.id, queue])

  if (!isAuthenticated || !user) {
    return null
  }

  if (!settings) {
    return null
  }

  const current = queue[0]
  const sessionDone = lessonStarted && totalInSession > 0 && queue.length === 0
  const indexInSession =
    totalInSession > 0 ? totalInSession - queue.length + 1 : 0

  return (
    <main className="lesson-page">
      <div
        className={`lesson-page__inner lesson-page__inner--wide${sessionDone ? ' lesson-page__inner--summary' : ''}`}
      >
        {loadError && (
          <div className="register-page__banner" role="alert">
            {loadError}
          </div>
        )}

        {loadingCards && !loadError && (
          <p className="lesson-page__status" aria-live="polite">
            Wczytywanie kart…
          </p>
        )}

        {!loadingCards &&
          !loadError &&
          cards &&
          cards.length === 0 &&
          !lessonStarted && (
            <div className="lesson-page__panel">
              <p className="lesson-page__panel-text">
                Brak kart spełniających te kryteria (np. powtórki pojawiły się w
                międzyczasie).
              </p>
              <Link to="/lesson-settings" className="lesson-page__link">
                ← Wróć do ustawień lekcji
              </Link>
            </div>
          )}

        {!loadingCards &&
          !loadError &&
          settings.lessonMode === 'typing' &&
          !lessonStarted &&
          cards &&
          cards.length > 0 && (
            <div className="lesson-page__panel lesson-page__panel--ready">
              <p className="lesson-page__ready-line">
                <strong>{cards.length}</strong>{' '}
                {cards.length === 1
                  ? 'słowo'
                  : cards.length >= 2 && cards.length <= 4
                    ? 'słowa'
                    : 'słów'}{' '}
                do sesji (limit: {settings.wordsInLesson}).
              </p>
              <p className="lesson-page__hint">
                Wpisuj dokładną odpowiedź (jak w polu „Odpowiedź” na karcie),
                zatwierdź Enter. Po sprawdzeniu Enter wybiera sugerowaną ocenę (przy dobrej
                odpowiedzi — „Wiem”, przy złej — „Nie wiem”); możesz też użyć strzałek.
              </p>
              <button
                type="button"
                className="lesson-page__start"
                disabled={startingLesson}
                onClick={() => void startLesson()}
              >
                {startingLesson ? 'Tworzenie lekcji…' : 'Rozpocznij lekcję'}
              </button>
            </div>
          )}

        {!loadingCards &&
          !loadError &&
          settings.lessonMode === 'flashcards' &&
          !lessonStarted &&
          cards &&
          cards.length > 0 && (
            <div className="lesson-page__panel lesson-page__panel--ready">
              <p className="lesson-page__ready-line">
                <strong>{cards.length}</strong>{' '}
                {cards.length === 1
                  ? 'słowo'
                  : cards.length >= 2 && cards.length <= 4
                    ? 'słowa'
                    : 'słów'}{' '}
                do sesji (limit: {settings.wordsInLesson}).
              </p>
              <p className="lesson-page__hint">
                Zobaczysz pytanie, odsłonisz odpowiedź, a potem oznaczysz, czy
                ją znałeś. Nieznane słowa wrócą na koniec kolejki.
              </p>
              <button
                type="button"
                className="lesson-page__start"
                disabled={startingLesson}
                onClick={() => void startLesson()}
              >
                {startingLesson ? 'Tworzenie lekcji…' : 'Rozpocznij lekcję'}
              </button>
            </div>
          )}

        {lessonStarted && settings.lessonMode === 'flashcards' && current && (
          <div className="lesson-flash">
            <div className="lesson-flash__top">
              <p className="lesson-flash__progress" aria-live="polite">
                Karta {indexInSession} z {totalInSession}
              </p>
              <button
                type="button"
                className="lesson-flash__tick"
                disabled={
                  tickBusy || tickDone || typeof current.questionResultId !== 'number'
                }
                title="Oznacz stronę pytania (tick) w bazie"
                onClick={() => void onTickResult()}
              >
                {tickBusy
                  ? '…'
                  : tickDone
                    ? '✓ Tick'
                    : 'Tick'}
              </button>
            </div>
            {tickError && (
              <p className="lesson-flash__tick-msg lesson-flash__tick-msg--error" role="alert">
                {tickError}
              </p>
            )}
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
                <button
                  type="button"
                  className="lesson-flash__reveal"
                  onClick={() => setAnswerRevealed(true)}
                >
                  Pokaż odpowiedź
                  <span className="lesson-flash__reveal-hint">Enter</span>
                </button>
              )}

              {answerRevealed && (
                <div
                  className="lesson-flash__grade"
                  role="group"
                  aria-label="Oceń, czy znałeś odpowiedź. Skróty: strzałka w lewo — nie znałem, w prawo — znałem."
                >
                  <button
                    type="button"
                    className="lesson-flash__btn lesson-flash__btn--dont"
                    title="Skrót: strzałka w lewo (←)"
                    onClick={onDidNotKnow}
                  >
                    Nie znałem
                  </button>
                  <button
                    type="button"
                    className="lesson-flash__btn lesson-flash__btn--know"
                    title="Skrót: strzałka w prawo (→)"
                    onClick={onKnew}
                  >
                    Znałem
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {lessonStarted && settings.lessonMode === 'typing' && current && (
          <div className="lesson-flash">
            <div className="lesson-flash__top">
              <p className="lesson-flash__progress" aria-live="polite">
                Karta {indexInSession} z {totalInSession}
              </p>
              <button
                type="button"
                className="lesson-flash__tick"
                disabled={
                  tickBusy || tickDone || typeof current.questionResultId !== 'number'
                }
                title="Oznacz stronę pytania (tick) w bazie"
                onClick={() => void onTickResult()}
              >
                {tickBusy
                  ? '…'
                  : tickDone
                    ? '✓ Tick'
                    : 'Tick'}
              </button>
            </div>
            {tickError && (
              <p className="lesson-flash__tick-msg lesson-flash__tick-msg--error" role="alert">
                {tickError}
              </p>
            )}
            <div className="lesson-flash__card" role="region" aria-label="Karta — tryb wpisywania">
              <div className="lesson-flash__block lesson-flash__block--question">
                <p className="lesson-flash__label">Pytanie</p>
                <p className="lesson-flash__text">{current.question.label}</p>
                {current.question.example.trim() !== '' && (
                  <p className="lesson-flash__example">{current.question.example}</p>
                )}
              </div>

              {!typingChecked && (
                <form
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
                  <button type="submit" className="lesson-flash__type-submit">
                    Zatwierdź
                    <span className="lesson-flash__reveal-hint">Enter</span>
                  </button>
                </form>
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
                  <div
                    className="lesson-flash__grade"
                    role="group"
                    aria-label="Jak oceniasz swoją znajomość? Enter — sugerowany wybór, strzałka w lewo — nie wiem, w prawo — wiem."
                  >
                    <button
                      type="button"
                      className={`lesson-flash__btn lesson-flash__btn--dont${typingExactMatch === false ? ' lesson-flash__btn--suggested' : ''}`}
                      title={
                        typingExactMatch === false
                          ? 'Skrót: Enter lub strzałka w lewo (←)'
                          : 'Skrót: strzałka w lewo (←)'
                      }
                      onClick={onDidNotKnow}
                    >
                      Nie wiem
                    </button>
                    <button
                      type="button"
                      className={`lesson-flash__btn lesson-flash__btn--know${typingExactMatch === true ? ' lesson-flash__btn--suggested' : ''}`}
                      title={
                        typingExactMatch === true
                          ? 'Skrót: Enter lub strzałka w prawo (→)'
                          : 'Skrót: strzałka w prawo (→)'
                      }
                      onClick={onKnew}
                    >
                      Wiem
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        )}

        {sessionDone && (
          <>
            <div className="lesson-page__panel lesson-page__panel--ready">
              <p className="lesson-page__ready-line">Lekcja ukończona</p>
              <p className="lesson-page__hint">
                Przerobiłeś wszystkie karty w tej sesji ({totalInSession}{' '}
                {totalInSession === 1 ? 'słowo' : 'słów'}).
              </p>
              <Link to="/lesson-settings" className="lesson-page__link">
                Nowa lekcja
              </Link>
            </div>

            <LessonPostHistory entries={answerHistory} />
          </>
        )}

        {!lessonStarted && (
          <Link to="/lesson-settings" className="lesson-page__back">
            ← Ustawienia lekcji
          </Link>
        )}
        {sessionDone && (
          <Link to="/dashboard" className="lesson-page__back">
            ← Pulpit
          </Link>
        )}
      </div>
    </main>
  )
}
