import { useEffect, useState } from 'react'
import { WordkiApiError } from '@wordki/shared'
import type { LessonWordSource } from '../lib/lessonTypes'
import { useWordkiBackend } from './useWordkiBackend'

export type DueTodayDirectionFilter = {
  questionSideType: string
  answerSideType: string
} | null

export type UseWordsDueTodayCountResult = {
  dueTodayCount: number | null
  loading: boolean
  error: string | null
  refetch: () => void
}

export type UseWordsDueTodayCountOptions = {
  /** Gdy `false`, nie wywołuje API (np. krok kreatora bez jeszcze wybranego źródła). */
  enabled?: boolean
}

/**
 * Liczba kart w puli lekcji — `GET /api/cards/due-today-count` (`wordSource`: powtórka vs nowe słowa).
 * Gdy `direction` jest ustawione, liczy tylko grupy pasujące do kierunku (jak w lekcji).
 */
export function useWordsDueTodayCount(
  userId: string | undefined,
  direction: DueTodayDirectionFilter = null,
  wordSource: LessonWordSource = 'review',
  options: UseWordsDueTodayCountOptions = {},
): UseWordsDueTodayCountResult {
  const enabled = options.enabled !== false
  const api = useWordkiBackend()
  const [dueTodayCount, setDueTodayCount] = useState<number | null>(null)
  const [loading, setLoading] = useState(Boolean(userId && enabled))
  const [error, setError] = useState<string | null>(null)
  const [refetchTick, setRefetchTick] = useState(0)

  const refetch = () => setRefetchTick((t) => t + 1)

  const q = direction?.questionSideType
  const a = direction?.answerSideType

  useEffect(() => {
    if (!enabled || !userId) {
      setLoading(false)
      setDueTodayCount(null)
      setError(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    void (async () => {
      try {
        const { dueTodayCount: n } = await api.getWordsDueTodayCount(
          userId,
          direction,
          wordSource,
        )
        if (cancelled) return
        setDueTodayCount(n)
        setError(null)
      } catch (err) {
        if (cancelled) return
        if (err instanceof WordkiApiError) {
          const missingProfile = err.errors.some(
            (x) => x.code === 'cards.get_due_today_count.user.not_found',
          )
          if (missingProfile) {
            setDueTodayCount(0)
            setError(null)
            return
          }
          setError(
            err.errors[0]?.message ??
              `Could not load due count (${err.status || 'error'}).`,
          )
          setDueTodayCount(null)
          return
        }
        setError(err instanceof Error ? err.message : 'Something went wrong.')
        setDueTodayCount(null)
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [api, userId, refetchTick, q, a, wordSource, enabled])

  return { dueTodayCount, loading, error, refetch }
}
