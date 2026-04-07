export type LoginFormValues = {
  email: string
  password: string
}

export type LoginFieldErrors = Partial<Record<keyof LoginFormValues, string>>

/** Aligns with backend `LoginUserCommandValidator` email rules. */
const EMAIL_REGEX = /^[^@\s]+@[^\s]+\.[^\s]+$/i

export function validateLoginForm(values: LoginFormValues): LoginFieldErrors {
  const errors: LoginFieldErrors = {}

  const email = values.email.trim()
  if (!email) {
    errors.email = 'Email is required.'
  } else if (!EMAIL_REGEX.test(email)) {
    errors.email = 'Please enter a valid email address.'
  }

  if (!values.password) {
    errors.password = 'Password is required.'
  }

  return errors
}
