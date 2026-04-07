import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  WordkiApiError,
  WordkiBackendService,
} from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import {
  hasFieldErrors,
  validateRegisterForm,
  type RegisterFormValues,
  type RegisterFieldErrors,
} from '../lib/registerValidation'
import './RegisterPage.css'

const initialValues: RegisterFormValues = {
  userName: '',
  email: '',
  password: '',
  confirmPassword: '',
}

export function RegisterPage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/', { replace: true })
    }
  }, [isAuthenticated, navigate])

  const [values, setValues] = useState<RegisterFormValues>(initialValues)
  const [fieldErrors, setFieldErrors] = useState<RegisterFieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const baseUrl = import.meta.env.VITE_BFF_BASE_URL ?? ''

  function updateField<K extends keyof RegisterFormValues>(
    key: K,
    value: RegisterFormValues[K],
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

    const nextErrors = validateRegisterForm(values)
    setFieldErrors(nextErrors)
    if (hasFieldErrors(nextErrors)) {
      return
    }

    setSubmitting(true)
    try {
      const api = new WordkiBackendService(baseUrl)
      await api.register({
        email: values.email.trim(),
        password: values.password,
        userName: values.userName.trim(),
      })
      navigate('/login', {
        state: { registered: true },
        replace: true,
      })
    } catch (err) {
      if (err instanceof WordkiApiError) {
        const apiFieldMap: RegisterFieldErrors = {}
        for (const item of err.errors) {
          const f = item.field?.toLowerCase()
          if (f === 'email') apiFieldMap.email = item.message
          else if (f === 'password') apiFieldMap.password = item.message
          else if (f === 'username') apiFieldMap.userName = item.message
        }
        const mapped = hasFieldErrors(apiFieldMap)
        setFieldErrors(apiFieldMap)
        setFormError(
          mapped
            ? null
            : err.errors[0]?.message ??
                `Registration failed (${err.status || 'error'}).`,
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
        <h1 className="register-page__title">Create account</h1>
        <p className="register-page__lead">
          Already have an account?{' '}
          <Link to="/login" className="register-page__link">
            Sign in
          </Link>
        </p>

        {formError && (
          <div className="register-page__banner" role="alert">
            {formError}
          </div>
        )}

        <form className="register-form" onSubmit={(e) => void handleSubmit(e)} noValidate>
          <div className="register-form__field">
            <label htmlFor="register-username">Username</label>
            <input
              id="register-username"
              name="userName"
              type="text"
              autoComplete="username"
              value={values.userName}
              onChange={(e) => updateField('userName', e.target.value)}
              className={fieldErrors.userName ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.userName}
              aria-describedby={
                fieldErrors.userName ? 'register-username-error' : undefined
              }
            />
            {fieldErrors.userName && (
              <span id="register-username-error" className="register-form__error" role="alert">
                {fieldErrors.userName}
              </span>
            )}
          </div>

          <div className="register-form__field">
            <label htmlFor="register-email">Email</label>
            <input
              id="register-email"
              name="email"
              type="email"
              autoComplete="email"
              value={values.email}
              onChange={(e) => updateField('email', e.target.value)}
              className={fieldErrors.email ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.email}
              aria-describedby={
                fieldErrors.email ? 'register-email-error' : undefined
              }
            />
            {fieldErrors.email && (
              <span id="register-email-error" className="register-form__error" role="alert">
                {fieldErrors.email}
              </span>
            )}
          </div>

          <div className="register-form__field">
            <label htmlFor="register-password">Password</label>
            <input
              id="register-password"
              name="password"
              type="password"
              autoComplete="new-password"
              value={values.password}
              onChange={(e) => updateField('password', e.target.value)}
              className={fieldErrors.password ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.password}
              aria-describedby={
                fieldErrors.password ? 'register-password-error' : undefined
              }
            />
            {fieldErrors.password && (
              <span id="register-password-error" className="register-form__error" role="alert">
                {fieldErrors.password}
              </span>
            )}
            <span className="register-form__hint">At least 8 characters.</span>
          </div>

          <div className="register-form__field">
            <label htmlFor="register-confirm">Confirm password</label>
            <input
              id="register-confirm"
              name="confirmPassword"
              type="password"
              autoComplete="new-password"
              value={values.confirmPassword}
              onChange={(e) => updateField('confirmPassword', e.target.value)}
              className={fieldErrors.confirmPassword ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.confirmPassword}
              aria-describedby={
                fieldErrors.confirmPassword
                  ? 'register-confirm-error'
                  : undefined
              }
            />
            {fieldErrors.confirmPassword && (
              <span id="register-confirm-error" className="register-form__error" role="alert">
                {fieldErrors.confirmPassword}
              </span>
            )}
          </div>

          <button
            type="submit"
            className="register-form__submit"
            disabled={submitting}
          >
            {submitting ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <Link to="/" className="register-page__back">
          ← Back to home
        </Link>
      </div>
    </main>
  )
}
