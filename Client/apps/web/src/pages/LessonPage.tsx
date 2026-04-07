import { useCallback, useEffect, useMemo, useState } from 'react'
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

  const startFlashcardLesson = useCallback(async () => {
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

  if (!isAuthenticated || !user) {
    return null
  }

  if (!settings) {
    return null
  }

  const dirLabel = `${settings.direction.questionSideType} → ${settings.direction.answerSideType}`
  const modeLabel =
    settings.lessonMode === 'flashcards' ? 'Tryb fiszek' : 'Tryb wpisywania'

  const current = queue[0]
  const sessionDone = lessonStarted && totalInSession > 0 && queue.length === 0
  const indexInSession =
    totalInSession > 0 ? totalInSession - queue.length + 1 : 0

  return (
    <main className="lesson-page">
      <div
        className={`lesson-page__inner lesson-page__inner--wide${sessionDone ? ' lesson-page__inner--summary' : ''}`}
      >
        <h1 className="lesson-page__title">Lekcja</h1>
        <p className="lesson-page__lead">
          {modeLabel}
          {' · '}
          {dirLabel}
        </p>

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
            <div className="lesson-page__panel">
              <p className="lesson-page__panel-text">
                Tryb wpisywania nie jest jeszcze dostępny. Wybierz tryb fiszek w
                ustawieniach lekcji.
              </p>
              <Link to="/lesson-settings" className="lesson-page__link">
                ← Ustawienia lekcji
              </Link>
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
                onClick={() => void startFlashcardLesson()}
              >
                {startingLesson ? 'Tworzenie lekcji…' : 'Rozpocznij lekcję'}
              </button>
            </div>
          )}

        {lessonStarted && settings.lessonMode === 'flashcards' && current && (
          <div className="lesson-flash">
            <p className="lesson-flash__progress" aria-live="polite">
              Karta {indexInSession} z {totalInSession}
            </p>
            <div className="lesson-flash__card" role="region" aria-label="Karta">
              <div className="lesson-flash__block lesson-flash__block--question">
                <p className="lesson-flash__label">Pytanie</p>
                <p className="lesson-flash__text">{current.question.label}</p>
                {current.question.example.trim() !== '' && (
                  <p className="lesson-flash__example">{current.question.example}</p>
                )}
              </div>

              {!answerRevealed && (
                <button
                  type="button"
                  className="lesson-flash__reveal"
                  onClick={() => setAnswerRevealed(true)}
                >
                  Pokaż odpowiedź
                </button>
              )}

              {answerRevealed && (
                <>
                  <div className="lesson-flash__block lesson-flash__block--answer">
                    <p className="lesson-flash__label">Odpowiedź</p>
                    <p className="lesson-flash__text">{current.answer.label}</p>
                    {current.answer.example.trim() !== '' && (
                      <p className="lesson-flash__example">{current.answer.example}</p>
                    )}
                  </div>
                  <div
                    className="lesson-flash__grade"
                    role="group"
                    aria-label="Oceń, czy znałeś odpowiedź"
                  >
                    <button
                      type="button"
                      className="lesson-flash__btn lesson-flash__btn--know"
                      onClick={onKnew}
                    >
                      Znałem
                    </button>
                    <button
                      type="button"
                      className="lesson-flash__btn lesson-flash__btn--dont"
                      onClick={onDidNotKnow}
                    >
                      Nie znałem
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
