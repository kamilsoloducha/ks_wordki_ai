import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import type { LoginUserResult } from '@wordki/shared'
import {
  clearAuthSession,
  persistAuthSession,
  readPersistedAuthSession,
  type PersistedAuthSession,
} from '../lib/authStorage'

export type AuthContextValue = {
  readonly user: PersistedAuthSession['user'] | null
  readonly accessToken: string | null
  readonly isAuthenticated: boolean
  signIn: (result: LoginUserResult) => void
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<PersistedAuthSession | null>(() =>
    readPersistedAuthSession(),
  )

  const signIn = useCallback((result: LoginUserResult) => {
    persistAuthSession(result)
    setSession({
      accessToken: result.accessToken,
      tokenType: result.tokenType,
      expiresAtUtc: result.expiresAtUtc,
      user: result.user,
    })
  }, [])

  const signOut = useCallback(() => {
    clearAuthSession()
    setSession(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      isAuthenticated: Boolean(session?.accessToken && session?.user),
      signIn,
      signOut,
    }),
    [session, signIn, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}
