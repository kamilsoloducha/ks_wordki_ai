import { useEffect, useState } from 'react'
import { WordkiApiError, type UserCardGroup } from '@wordki/shared'
import { useWordkiBackend } from './useWordkiBackend'

export type UseUserCardGroupsResult = {
  groups: UserCardGroup[]
  loading: boolean
  error: string | null
  refetch: () => void
}

/**
 * Loads card groups for the given app user id (`ExternalUserId` in the cards module).
 */
export function useUserCardGroups(
  userId: string | undefined,
): UseUserCardGroupsResult {
  const api = useWordkiBackend()
  const [groups, setGroups] = useState<UserCardGroup[]>([])
  const [loading, setLoading] = useState(Boolean(userId))
  const [error, setError] = useState<string | null>(null)
  const [refetchTick, setRefetchTick] = useState(0)

  const refetch = () => setRefetchTick((t) => t + 1)

  useEffect(() => {
    if (!userId) {
      setLoading(false)
      setGroups([])
      setError(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    void (async () => {
      try {
        const list = await api.getUserCardGroups(userId)
        if (!cancelled) {
          setGroups(list)
          setError(null)
        }
      } catch (err) {
        if (cancelled) return
        if (err instanceof WordkiApiError) {
          const missingProfile = err.errors.some(
            (x) => x.code === 'cards.get_groups.user.not_found',
          )
          if (missingProfile) {
            setGroups([])
            setError(null)
            return
          }
          setError(
            err.errors[0]?.message ??
              `Could not load groups (${err.status || 'error'}).`,
          )
          setGroups([])
          return
        }
        setError(err instanceof Error ? err.message : 'Something went wrong.')
        setGroups([])
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

  return { groups, loading, error, refetch }
}
