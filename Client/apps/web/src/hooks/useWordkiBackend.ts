import { useMemo } from 'react'
import { WordkiBackendService } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'

/** Axios client with BFF base URL and current session bearer token (if any). */
export function useWordkiBackend(): WordkiBackendService {
  const { accessToken } = useAuth()
  const baseUrl = import.meta.env.VITE_BFF_BASE_URL ?? ''

  return useMemo(() => {
    const api = new WordkiBackendService(baseUrl)
    api.setAccessToken(accessToken)
    return api
  }, [baseUrl, accessToken])
}
