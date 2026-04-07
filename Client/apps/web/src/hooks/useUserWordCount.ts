import { useEffect, useState } from 'react'
import { WordkiApiError } from '@wordki/shared'
import { useWordkiBackend } from './useWordkiBackend'

export type UseUserWordCountResult = {
  wordCount: number | null
  loading: boolean
  error: string | null
  refetch: () => void
}

/** Total card (word) count for the user via `GET /api/cards/words-count`. */
export function useUserWordCount(
  userId: string | undefined,
): UseUserWordCountResult {
  const api = useWordkiBackend()
  const [wordCount, setWordCount] = useState<number | null>(null)
  const [loading, setLoading] = useState(Boolean(userId))
  const [error, setError] = useState<string | null>(null)
  const [refetchTick, setRefetchTick] = useState(0)

  const refetch = () => setRefetchTick((t) => t + 1)

  useEffect(() => {
    if (!userId) {
      setLoading(false)
      setWordCount(null)
      setError(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    void (async () => {
      try {
        const { wordCount: n } = await api.getUserWordCount(userId)
        if (!cancelled) {
          setWordCount(n)
          setError(null)
        }
      } catch (err) {
        if (cancelled) return
        if (err instanceof WordkiApiError) {
          const missingProfile = err.errors.some(
            (x) => x.code === 'cards.get_word_count.user.not_found',
          )
          if (missingProfile) {
            setWordCount(0)
            setError(null)
            return
          }
          setError(
            err.errors[0]?.message ??
              `Could not load word count (${err.status || 'error'}).`,
          )
          setWordCount(null)
          return
        }
        setError(err instanceof Error ? err.message : 'Something went wrong.')
        setWordCount(null)
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [api, userId, refetchTick])

  return { wordCount, loading, error, refetch }
}
