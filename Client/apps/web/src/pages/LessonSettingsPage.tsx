import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { WordkiApiError, type SideTypePairDto } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import { useWordkiBackend } from '../hooks/useWordkiBackend'
import { useWordsDueTodayCount } from '../hooks/useWordsDueTodayCount'
import type {
  LessonMode,
  LessonQuestionDirection,
  LessonWordSource,
} from '../lib/lessonTypes'
import './LessonSettingsPage.css'
import './RegisterPage.css'

function polishWordForms(n: number): string {
  if (n === 1) {
    return 'słowo'
  }
  const mod10 = n % 10
  const mod100 = n % 100
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) {
    return 'słowa'
  }
  return 'słów'
}

function directionKey(d: LessonQuestionDirection): string {
  return `${d.questionSideType}\0${d.answerSideType}`
}

function directedOptionsFromPairs(pairs: SideTypePairDto[]): LessonQuestionDirection[] {
  const out: LessonQuestionDirection[] = []
  for (const p of pairs) {
    if (p.sideType1 === p.sideType2) {
      out.push({
        questionSideType: p.sideType1,
        answerSideType: p.sideType2,
      })
    } else {
      out.push(
        {
          questionSideType: p.sideType1,
          answerSideType: p.sideType2,
        },
        {
          questionSideType: p.sideType2,
          answerSideType: p.sideType1,
        },
      )
    }
  }
  out.sort((a, b) => directionKey(a).localeCompare(directionKey(b), undefined, { sensitivity: 'base' }))
  return out
}

function sameDirection(
  a: LessonQuestionDirection,
  b: LessonQuestionDirection,
): boolean {
  return (
    a.questionSideType === b.questionSideType &&
    a.answerSideType === b.answerSideType
  )
}

function modeLabel(mode: LessonMode): string {
  return mode === 'flashcards' ? 'Tryb fiszek' : 'Tryb wpisywania'
}

function wordSourceLabel(ws: LessonWordSource): string {
  return ws === 'review' ? 'Powtórka' : 'Nauka nowych słów'
}

export function LessonSettingsPage() {
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()
  const api = useWordkiBackend()

  const [wizardStep, setWizardStep] = useState<1 | 2 | 3 | 4>(1)
  const [wordSource, setWordSource] = useState<LessonWordSource | null>(null)
  const [lessonMode, setLessonMode] = useState<LessonMode | null>(null)
  const [direction, setDirection] = useState<LessonQuestionDirection | null>(null)
  const [wordsInLesson, setWordsInLesson] = useState(20)

  const [pairsLoading, setPairsLoading] = useState(true)
  const [pairsError, setPairsError] = useState<string | null>(null)
  const [sideTypePairs, setSideTypePairs] = useState<SideTypePairDto[]>([])

  const directedOptions = useMemo(
    () => directedOptionsFromPairs(sideTypePairs),
    [sideTypePairs],
  )

  const { dueTodayCount: reviewPoolCount, loading: loadingReview, error: errReview } =
    useWordsDueTodayCount(user?.id, null, 'review', { enabled: wizardStep === 1 })

  const { dueTodayCount: newPoolCount, loading: loadingNew, error: errNew } =
    useWordsDueTodayCount(user?.id, null, 'newWords', { enabled: wizardStep === 1 })

  const countDirection = wizardStep >= 3 ? direction : null
  const { dueTodayCount, loading, error } = useWordsDueTodayCount(
    user?.id,
    countDirection,
    wordSource ?? 'review',
    { enabled: wizardStep >= 2 && wordSource !== null },
  )

  const available = dueTodayCount ?? 0
  const maxWords = useMemo(() => Math.max(0, available), [available])

  const step1Loading = loadingReview || loadingNew
  const step1Error = errReview ?? errNew

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: '/lesson-settings' } })
    }
  }, [isAuthenticated, navigate])

  useEffect(() => {
    if (!user?.id) {
      return
    }

    let cancelled = false
    setPairsLoading(true)
    setPairsError(null)

    void (async () => {
      try {
        const pairs = await api.getDistinctSideTypePairs(user.id)
        if (cancelled) return
        setSideTypePairs(pairs)
        setPairsError(null)
      } catch (err) {
        if (cancelled) return
        if (err instanceof WordkiApiError) {
          const missingProfile = err.errors.some(
            (x) => x.code === 'cards.get_side_type_pairs.user.not_found',
          )
          if (missingProfile) {
            setSideTypePairs([])
            setPairsError(null)
          } else {
            setSideTypePairs([])
            setPairsError(
              err.errors[0]?.message ??
                `Nie udało się wczytać par języków (${err.status ?? 'błąd'}).`,
            )
          }
        } else {
          setSideTypePairs([])
          setPairsError(
            err instanceof Error ? err.message : 'Nie udało się wczytać par języków.',
          )
        }
      } finally {
        if (!cancelled) {
          setPairsLoading(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [api, user?.id])

  useEffect(() => {
    if (directedOptions.length === 0) {
      setDirection(null)
      return
    }
    setDirection((prev) => {
      if (prev && directedOptions.some((o) => sameDirection(o, prev))) {
        return prev
      }
      return null
    })
  }, [directedOptions])

  useEffect(() => {
    if (maxWords <= 0) {
      setWordsInLesson(0)
      return
    }
    setWordsInLesson((prev) => {
      const next = prev <= 0 ? maxWords : Math.min(prev, maxWords)
      return Math.max(1, Math.min(next, maxWords))
    })
  }, [maxWords])

  if (!isAuthenticated || !user) {
    return null
  }

  function onWordsInput(raw: string) {
    const n = Number.parseInt(raw, 10)
    if (!Number.isFinite(n) || maxWords <= 0) return
    setWordsInLesson(Math.min(maxWords, Math.max(1, n)))
  }

  function selectDirectionAndContinue(opt: LessonQuestionDirection) {
    setDirection(opt)
    setWizardStep(4)
  }

  function resetFromSourceStep() {
    setWizardStep(1)
    setWordSource(null)
    setLessonMode(null)
    setDirection(null)
  }

  const startDisabled =
    pairsLoading ||
    !wordSource ||
    !direction ||
    maxWords <= 0 ||
    loading ||
    !lessonMode ||
    wizardStep !== 4

  const poolHintEmpty =
    wordSource === 'review'
      ? 'Nie masz kart z terminem powtórki do końca dziś (UTC) dla tego kierunku. Dodaj karty lub wybierz inny kierunek albo źródło słów.'
      : 'Nie masz kart, na których strona pytania nie ma jeszcze ustalonej powtórki (nowe słowa). Dodaj karty lub wybierz inny kierunek.'

  const poolMetaWhenEmpty =
    wordSource === 'review'
      ? 'Brak słów do wyboru — pojawią się po zaplanowanych powtórkach.'
      : 'Brak nowych słów w tej puli — wybierz powtórkę lub inny kierunek.'

  return (
    <main className="lesson-settings">
      <div className="lesson-settings__inner">
        <h1 className="lesson-settings__title">Ustawienia lekcji</h1>
        <p className="lesson-settings__lead">
          Dopasuj sposób nauki przed startem. Powtórki: karty z terminem do końca bieżącego dnia UTC
          (w tym zaległe). Nowe słowa: strona pytania bez jeszcze zaplanowanej powtórki w tym kierunku.
        </p>

        {(error || pairsError || step1Error) && (
          <div className="lesson-settings__banner" role="alert">
            {[error, pairsError, step1Error].filter(Boolean).join(' ')}
          </div>
        )}

        {wizardStep === 1 && (
          <p className="lesson-settings__step-intro" id="lesson-step-label">
            Krok 1 z 4 — skąd bierzemy słowa
          </p>
        )}

        {wizardStep === 1 && (
          <p className="lesson-settings__inline-count" aria-live="polite">
            {step1Loading
              ? 'Ładowanie liczników…'
              : `Powtórka (wszystkie kierunki, UTC): ${reviewPoolCount ?? 0} ${polishWordForms(reviewPoolCount ?? 0)} · Nowe słowa: ${newPoolCount ?? 0} ${polishWordForms(newPoolCount ?? 0)}`}
          </p>
        )}

        {wizardStep === 1 && (
          <section
            className="lesson-settings__section lesson-settings__section--active"
            aria-labelledby="lesson-source-heading"
          >
            <h2 id="lesson-source-heading" className="lesson-settings__section-title">
              Źródło słów
            </h2>
            <p className="lesson-settings__section-hint">
              Powtórka korzysta z kart, które mają już zaplanowaną następną powtórkę na dziś (UTC).
              Nowe słowa — gdy na stronie pytania nie ma jeszcze harmonogramu powtórki.
            </p>
            <div className="lesson-settings__options" role="radiogroup" aria-label="Źródło słów">
              <label
                className={`lesson-settings__option ${wordSource === 'review' ? 'lesson-settings__option--selected' : ''}`}
              >
                <input
                  type="radio"
                  name="wordSource"
                  checked={wordSource === 'review'}
                  onChange={() => {
                    setWordSource('review')
                    setWizardStep(2)
                  }}
                />
                <span className="lesson-settings__option-body">
                  <span className="lesson-settings__option-title">Powtórka</span>
                  <p className="lesson-settings__option-desc">
                    Termin powtórki ustawiony i przypada na dziś lub wcześniej (UTC).
                  </p>
                </span>
              </label>
              <label
                className={`lesson-settings__option ${wordSource === 'newWords' ? 'lesson-settings__option--selected' : ''}`}
              >
                <input
                  type="radio"
                  name="wordSource"
                  checked={wordSource === 'newWords'}
                  onChange={() => {
                    setWordSource('newWords')
                    setWizardStep(2)
                  }}
                />
                <span className="lesson-settings__option-body">
                  <span className="lesson-settings__option-title">Nauka nowych słów</span>
                  <p className="lesson-settings__option-desc">
                    Strona pytania bez zaplanowanej powtórki — pierwsze przechodzenie w tym kierunku.
                  </p>
                </span>
              </label>
            </div>
          </section>
        )}

        {wizardStep >= 2 && wordSource && (
          <div className="lesson-settings__picked">
            <div className="lesson-settings__picked-head">
              <div>
                <p className="lesson-settings__picked-label">Źródło słów</p>
                <p className="lesson-settings__picked-value">{wordSourceLabel(wordSource)}</p>
                <p className="lesson-settings__picked-desc">
                  {wordSource === 'review'
                    ? 'Karty z powtórką do końca dziś (UTC).'
                    : 'Karty, gdzie strona pytania nie ma jeszcze terminu powtórki.'}
                </p>
              </div>
              <button type="button" className="lesson-settings__change" onClick={resetFromSourceStep}>
                Zmień
              </button>
            </div>
          </div>
        )}

        {wizardStep === 2 && (
          <p className="lesson-settings__step-intro">Krok 2 z 4 — wybierz rodzaj lekcji</p>
        )}

        {wizardStep === 2 && (
          <section
            className="lesson-settings__section lesson-settings__section--active"
            aria-labelledby="lesson-mode-heading"
          >
            <h2 id="lesson-mode-heading" className="lesson-settings__section-title">
              Rodzaj lekcji
            </h2>
            <p className="lesson-settings__section-hint">
              Tryb fiszek: sam oceniasz, czy znasz odpowiedź. Tryb wpisywania: wpisujesz odpowiedź,
              aplikacja ją porównuje, potem oceniasz, czy ją pamiętałeś.
            </p>
            <div className="lesson-settings__options" role="radiogroup" aria-label="Rodzaj lekcji">
              <label
                className={`lesson-settings__option ${lessonMode === 'flashcards' ? 'lesson-settings__option--selected' : ''}`}
              >
                <input
                  type="radio"
                  name="lessonMode"
                  checked={lessonMode === 'flashcards'}
                  onChange={() => {
                    setLessonMode('flashcards')
                    setWizardStep(3)
                  }}
                />
                <span className="lesson-settings__option-body">
                  <span className="lesson-settings__option-title">Tryb fiszek</span>
                  <p className="lesson-settings__option-desc">
                    Pokazujesz kartę, myślisz, potwierdzasz, czy znasz znaczenie — tempo ustawiasz
                    Ty.
                  </p>
                </span>
              </label>
              <label
                className={`lesson-settings__option ${lessonMode === 'typing' ? 'lesson-settings__option--selected' : ''}`}
              >
                <input
                  type="radio"
                  name="lessonMode"
                  checked={lessonMode === 'typing'}
                  onChange={() => {
                    setLessonMode('typing')
                    setWizardStep(3)
                  }}
                />
                <span className="lesson-settings__option-body">
                  <span className="lesson-settings__option-title">Tryb wpisywania</span>
                  <p className="lesson-settings__option-desc">
                    Wpisujesz tłumaczenie; aplikacja porówna odpowiedź z oczekiwaną.
                  </p>
                </span>
              </label>
            </div>
          </section>
        )}

        {wizardStep >= 3 && lessonMode && (
          <div className="lesson-settings__picked">
            <div className="lesson-settings__picked-head">
              <div>
                <p className="lesson-settings__picked-label">Rodzaj lekcji</p>
                <p className="lesson-settings__picked-value">{modeLabel(lessonMode)}</p>
                <p className="lesson-settings__picked-desc">
                  {lessonMode === 'flashcards'
                    ? 'Sam oceniasz, czy znasz odpowiedź; tempo ustawiasz Ty.'
                    : 'Wpisujesz odpowiedź; po sprawdzeniu oceniasz, czy ją pamiętałeś.'}
                </p>
              </div>
              <button
                type="button"
                className="lesson-settings__change"
                onClick={() => {
                  setWizardStep(2)
                  setLessonMode(null)
                  setDirection(null)
                }}
              >
                Zmień
              </button>
            </div>
          </div>
        )}

        {wizardStep === 3 && (
          <p className="lesson-settings__step-intro">Krok 3 z 4 — kierunek nauki</p>
        )}

        {wizardStep === 3 && (
          <section
            className="lesson-settings__section lesson-settings__section--active"
            aria-labelledby="lesson-direction-heading"
          >
            <h2 id="lesson-direction-heading" className="lesson-settings__section-title">
              Kierunek pytania
            </h2>
            <p className="lesson-settings__section-hint">
              Lista opiera się na parach typów stron z Twoich grup (front/back). Dla każdej pary
              możesz ćwiczyć w obie strony — np. <strong>PL → EN</strong> lub{' '}
              <strong>EN → PL</strong>.
            </p>
            {pairsLoading && (
              <p className="lesson-settings__section-hint" aria-live="polite">
                Wczytywanie dostępnych kierunków…
              </p>
            )}
            {!pairsLoading && !pairsError && sideTypePairs.length === 0 && (
              <p className="lesson-settings__section-hint" role="status">
                Nie masz jeszcze grup z ustawionymi typami stron — dodaj grupę w „Grupy”, wtedy
                pojawią się tu dostępne kierunki.
              </p>
            )}
            {!pairsLoading && directedOptions.length > 0 && (
              <div
                className="lesson-settings__options"
                role="radiogroup"
                aria-label="Kierunek pytania"
              >
                {directedOptions.map((opt) => {
                  const key = directionKey(opt)
                  const selected =
                    direction !== null && sameDirection(direction, opt)
                  return (
                    <label
                      key={key}
                      className={`lesson-settings__option ${selected ? 'lesson-settings__option--selected' : ''}`}
                      onClick={() => selectDirectionAndContinue(opt)}
                    >
                      <input
                        type="radio"
                        name="lessonDirection"
                        checked={selected}
                        onChange={() => selectDirectionAndContinue(opt)}
                      />
                      <span className="lesson-settings__option-body">
                        <span className="lesson-settings__option-title lesson-settings__option-title--direction">
                          <span className="lesson-settings__side-type">
                            {opt.questionSideType}
                          </span>
                          <span className="lesson-settings__arrow" aria-hidden>
                            →
                          </span>
                          <span className="lesson-settings__side-type">
                            {opt.answerSideType}
                          </span>
                        </span>
                        <p className="lesson-settings__option-desc">
                          Pytanie po stronie „{opt.questionSideType}”, odpowiedź po stronie
                          „{opt.answerSideType}”.
                        </p>
                      </span>
                    </label>
                  )
                })}
              </div>
            )}
          </section>
        )}

        {wizardStep >= 4 && lessonMode && direction && (
          <div className="lesson-settings__picked">
            <div className="lesson-settings__picked-head">
              <div>
                <p className="lesson-settings__picked-label">Kierunek nauki</p>
                <p className="lesson-settings__picked-value lesson-settings__picked-value--direction">
                  <span className="lesson-settings__side-type">
                    {direction.questionSideType}
                  </span>
                  <span className="lesson-settings__arrow" aria-hidden>
                    →
                  </span>
                  <span className="lesson-settings__side-type">
                    {direction.answerSideType}
                  </span>
                </p>
                <p className="lesson-settings__picked-desc">
                  Pytanie z pierwszej strony, odpowiedź z drugiej (jak w grupach).
                </p>
              </div>
              <button
                type="button"
                className="lesson-settings__change"
                onClick={() => setWizardStep(3)}
              >
                Zmień
              </button>
            </div>
          </div>
        )}

        {wizardStep === 4 && (
          <p className="lesson-settings__step-intro">Krok 4 z 4 — wielkość lekcji</p>
        )}

        {wizardStep >= 3 && (
          <section
            className={`lesson-settings__summary ${wizardStep === 4 ? 'lesson-settings__summary--spaced' : ''}`}
            aria-labelledby="lesson-words-heading"
          >
            <p id="lesson-words-heading" className="lesson-settings__summary-label">
              Słowa w puli
              {wizardStep >= 4
                ? ' (dla wybranego kierunku i źródła)'
                : wizardStep === 3 && direction === null
                  ? ' (wszystkie kierunki, UTC)'
                  : ''}
            </p>
            <p className="lesson-settings__summary-value">
              {loading ? (
                '…'
              ) : (
                <>
                  {available}{' '}
                  <span className="lesson-settings__summary-unit">
                    {polishWordForms(available)}
                  </span>
                </>
              )}
            </p>
            {!loading && available === 0 && !error && wizardStep >= 4 && (
              <p className="lesson-settings__summary-hint">{poolHintEmpty}</p>
            )}
            {!loading && available > 0 && wizardStep >= 4 && (
              <p className="lesson-settings__summary-hint">
                To maksymalna pula dla tego kierunku — poniżej ustawisz, ile kart wziąć do sesji.
              </p>
            )}
            {wizardStep === 3 && !loading && (
              <p className="lesson-settings__summary-hint">
                {direction === null
                  ? 'Kliknij jeden z kierunków w sekcji poniżej — od razu przejdziesz do ustawienia liczby słów. Licznik powyżej to na razie suma dla wszystkich kierunków.'
                  : 'Kliknij ten sam lub inny kierunek poniżej, aby przejść dalej. Licznik uwzględnia podglądowo zaznaczony kierunek.'}
              </p>
            )}
          </section>
        )}

        {wizardStep === 4 && (
          <section className="lesson-settings__section" aria-labelledby="lesson-size-heading">
            <h2 id="lesson-size-heading" className="lesson-settings__section-title">
              Ile słów w lekcji
            </h2>
            <p className="lesson-settings__section-hint">
              Liczba kart w jednej sesji (nie więcej niż w puli: {loading ? '…' : available}).
            </p>
            <div className="lesson-settings__word-row">
              <div className="lesson-settings__word-input-wrap">
                <input
                  type="number"
                  min={maxWords > 0 ? 1 : 0}
                  max={maxWords > 0 ? maxWords : 0}
                  value={maxWords > 0 ? wordsInLesson : 0}
                  disabled={maxWords <= 0 || loading}
                  onChange={(e) => onWordsInput(e.target.value)}
                  aria-label="Liczba słów w lekcji"
                />
                <input
                  type="range"
                  min={1}
                  max={Math.max(1, maxWords)}
                  value={maxWords > 0 ? Math.min(wordsInLesson, maxWords) : 1}
                  disabled={maxWords <= 0 || loading}
                  onChange={(e) =>
                    setWordsInLesson(
                      Math.min(maxWords, Math.max(1, Number(e.target.value))),
                    )
                  }
                  aria-label="Suwak liczby słów w lekcji"
                />
              </div>
              <p className="lesson-settings__word-meta">
                {maxWords <= 0 ? poolMetaWhenEmpty : `W tej lekcji użyjesz ${wordsInLesson} z ${maxWords} dostępnych słów.`}
              </p>
            </div>
          </section>
        )}

        {wizardStep === 4 && (
          <div className="lesson-settings__cta">
            <button
              type="button"
              className="lesson-settings__start"
              disabled={startDisabled}
              onClick={() =>
                direction &&
                lessonMode &&
                wordSource &&
                navigate('/lesson', {
                  state: {
                    settings: {
                      wordSource,
                      lessonMode,
                      direction,
                      wordsInLesson,
                    },
                  },
                })
              }
            >
              Rozpocznij lekcję
            </button>
          </div>
        )}

        <Link to="/dashboard" className="lesson-settings__back">
          ← Wróć do pulpitu
        </Link>
      </div>
    </main>
  )
}
