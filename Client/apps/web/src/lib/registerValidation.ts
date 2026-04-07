export type RegisterFormValues = {
  userName: string
  email: string
  password: string
  confirmPassword: string
}

export type RegisterFieldErrors = Partial<
  Record<keyof RegisterFormValues, string>
>

/** Mirrors backend `RegisterUserCommandValidator` email pattern + password rules. */
const EMAIL_REGEX = /^[^@\s]+@[^\s]+\.[^\s]+$/i

const USERNAME_MAX = 100

export function validateRegisterForm(
  values: RegisterFormValues,
): RegisterFieldErrors {
  const errors: RegisterFieldErrors = {}

  const userName = values.userName.trim()
  if (!userName) {
    errors.userName = 'Username is required.'
  } else if (userName.length > USERNAME_MAX) {
    errors.userName = `Username cannot exceed ${USERNAME_MAX} characters.`
  }

  const email = values.email.trim()
  if (!email) {
    errors.email = 'Email is required.'
  } else if (!EMAIL_REGEX.test(email)) {
    errors.email = 'Please enter a valid email address.'
  }

  const password = values.password
  if (!password) {
    errors.password = 'Password is required.'
  } else if (password.length < 8) {
    errors.password = 'Password must be at least 8 characters.'
  }

  const confirm = values.confirmPassword
  if (!confirm) {
    errors.confirmPassword = 'Please confirm your password.'
  } else if (confirm !== password) {
    errors.confirmPassword = 'Passwords do not match.'
  }

  return errors
}

export function hasFieldErrors(
  errors: Record<string, string | undefined>,
): boolean {
  return Object.keys(errors).length > 0
}
