import type { CurrentUser, LoginUserResult } from '@wordki/shared'

const LEGACY_TOKEN_KEY = 'wordki_access_token'
const SESSION_KEY = 'wordki_auth_session'

export type PersistedAuthSession = {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  user: CurrentUser
}

function isPersistedSession(x: unknown): x is PersistedAuthSession {
  if (x === null || typeof x !== 'object') return false
  const o = x as Record<string, unknown>
  const u = o.user
  if (u === null || typeof u !== 'object') return false
  const user = u as Record<string, unknown>
  return (
    typeof o.accessToken === 'string' &&
    typeof o.tokenType === 'string' &&
    typeof o.expiresAtUtc === 'string' &&
    typeof user.id === 'string' &&
    typeof user.email === 'string' &&
    typeof user.role === 'string' &&
    typeof user.status === 'string'
  )
}

export function readPersistedAuthSession(): PersistedAuthSession | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY)
    if (raw) {
      const parsed: unknown = JSON.parse(raw)
      if (isPersistedSession(parsed)) {
        return parsed
      }
      localStorage.removeItem(SESSION_KEY)
    }
    localStorage.removeItem(LEGACY_TOKEN_KEY)
    return null
  } catch {
    try {
      localStorage.removeItem(SESSION_KEY)
      localStorage.removeItem(LEGACY_TOKEN_KEY)
    } catch {
      /* ignore */
    }
    return null
  }
}

export function persistAuthSession(result: LoginUserResult): void {
  const session: PersistedAuthSession = {
    accessToken: result.accessToken,
    tokenType: result.tokenType,
    expiresAtUtc: result.expiresAtUtc,
    user: result.user,
  }
  try {
    localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    localStorage.removeItem(LEGACY_TOKEN_KEY)
  } catch {
    /* private mode / quota */
  }
}

export function clearAuthSession(): void {
  try {
    localStorage.removeItem(SESSION_KEY)
    localStorage.removeItem(LEGACY_TOKEN_KEY)
  } catch {
    /* ignore */
  }
}
