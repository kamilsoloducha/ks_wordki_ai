import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { WordkiApiError, WordkiBackendService } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import { hasFieldErrors } from '../lib/registerValidation'
import {
  validateLoginForm,
  type LoginFormValues,
  type LoginFieldErrors,
} from '../lib/loginValidation'
import './RegisterPage.css'

const initialValues: LoginFormValues = {
  email: '',
  password: '',
}

/** Safe in-app path from login redirect state, or dashboard by default. */
function getPostLoginPath(state: unknown): string {
  const from = (state as { from?: unknown } | null)?.from
  if (
    typeof from === 'string' &&
    from.startsWith('/') &&
    !from.startsWith('//')
  ) {
    return from
  }
  return '/dashboard'
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, signIn } = useAuth()

  useEffect(() => {
    if (isAuthenticated) {
      navigate(getPostLoginPath(location.state), { replace: true })
    }
  }, [isAuthenticated, location.state, navigate])
  const registered = Boolean(
    (location.state as { registered?: boolean } | null)?.registered,
  )

  const [values, setValues] = useState<LoginFormValues>(initialValues)
  const [fieldErrors, setFieldErrors] = useState<LoginFieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const baseUrl = import.meta.env.VITE_BFF_BASE_URL ?? ''

  function updateField<K extends keyof LoginFormValues>(
    key: K,
    value: LoginFormValues[K],
  ) {
    setValues((v) => ({ ...v, [key]: value }))
    setFieldErrors((e) => {
      const next = { ...e }
      delete next[key]
      return next
    })
    setFormError(null)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError(null)

    const nextErrors = validateLoginForm(values)
    setFieldErrors(nextErrors)
    if (hasFieldErrors(nextErrors)) {
      return
    }

    setSubmitting(true)
    try {
      const api = new WordkiBackendService(baseUrl)
      const result = await api.login({
        email: values.email.trim(),
        password: values.password,
      })
      signIn(result)
      navigate(getPostLoginPath(location.state), { replace: true })
    } catch (err) {
      if (err instanceof WordkiApiError) {
        const apiFieldMap: LoginFieldErrors = {}
        for (const item of err.errors) {
          const f = item.field?.toLowerCase()
          if (f === 'email') apiFieldMap.email = item.message
          else if (f === 'password') apiFieldMap.password = item.message
        }
        const mapped = hasFieldErrors(apiFieldMap)
        setFieldErrors(apiFieldMap)
        setFormError(
          mapped
            ? null
            : err.errors[0]?.message ??
                `Sign in failed (${err.status || 'error'}).`,
        )
      } else {
        setFormError(
          err instanceof Error ? err.message : 'Something went wrong.',
        )
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (isAuthenticated) {
    return null
  }

  return (
    <main className="register-page">
      <div className="register-page__card">
        <h1 className="register-page__title">Sign in</h1>
        <p className="register-page__lead">
          New here?{' '}
          <Link to="/register" className="register-page__link">
            Create an account
          </Link>
        </p>

        {registered && (
          <div className="register-page__banner--success" role="status">
            Account created successfully. You can sign in now.
          </div>
        )}

        {formError && (
          <div className="register-page__banner" role="alert">
            {formError}
          </div>
        )}

        <form className="register-form" onSubmit={(e) => void handleSubmit(e)} noValidate>
          <div className="register-form__field">
            <label htmlFor="login-email">Email</label>
            <input
              id="login-email"
              name="email"
              type="email"
              autoComplete="email"
              value={values.email}
              onChange={(e) => updateField('email', e.target.value)}
              className={fieldErrors.email ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.email}
              aria-describedby={
                fieldErrors.email ? 'login-email-error' : undefined
              }
            />
            {fieldErrors.email && (
              <span id="login-email-error" className="register-form__error" role="alert">
                {fieldErrors.email}
              </span>
            )}
          </div>

          <div className="register-form__field">
            <label htmlFor="login-password">Password</label>
            <input
              id="login-password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={values.password}
              onChange={(e) => updateField('password', e.target.value)}
              className={fieldErrors.password ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.password}
              aria-describedby={
                fieldErrors.password ? 'login-password-error' : undefined
              }
            />
            {fieldErrors.password && (
              <span
                id="login-password-error"
                className="register-form__error"
                role="alert"
              >
                {fieldErrors.password}
              </span>
            )}
          </div>

          <button
            type="submit"
            className="register-form__submit"
            disabled={submitting}
          >
            {submitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <Link to="/" className="register-page__back">
          ← Back to home
        </Link>
      </div>
    </main>
  )
}
