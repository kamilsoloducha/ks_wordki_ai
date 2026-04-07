export type CardFormValues = {
  frontLabel: string
  backLabel: string
  frontExample: string
  frontComment: string
  backExample: string
  backComment: string
}

export type CardFormFieldErrors = Partial<Record<keyof CardFormValues, string>>

/** Mirrors cards module add/update validators. */
const LABEL_MAX = 200
const EXAMPLE_MAX = 1000
const COMMENT_MAX = 1000

function checkOptionalMax(
  value: string,
  max: number,
  label: string,
): string | null {
  const t = value.trim()
  if (!t) {
    return null
  }
  if (t.length > max) {
    return `${label} cannot exceed ${max} characters.`
  }
  return null
}

export function validateCardForm(values: CardFormValues): CardFormFieldErrors {
  const errors: CardFormFieldErrors = {}

  const fl = values.frontLabel.trim()
  if (!fl) {
    errors.frontLabel = 'Front label is required.'
  } else if (fl.length > LABEL_MAX) {
    errors.frontLabel = `Front label cannot exceed ${LABEL_MAX} characters.`
  }

  const bl = values.backLabel.trim()
  if (!bl) {
    errors.backLabel = 'Back label is required.'
  } else if (bl.length > LABEL_MAX) {
    errors.backLabel = `Back label cannot exceed ${LABEL_MAX} characters.`
  }

  const fe = checkOptionalMax(
    values.frontExample,
    EXAMPLE_MAX,
    'Front example',
  )
  if (fe) {
    errors.frontExample = fe
  }

  const fc = checkOptionalMax(
    values.frontComment,
    COMMENT_MAX,
    'Front comment',
  )
  if (fc) {
    errors.frontComment = fc
  }

  const be = checkOptionalMax(values.backExample, EXAMPLE_MAX, 'Back example')
  if (be) {
    errors.backExample = be
  }

  const bc = checkOptionalMax(values.backComment, COMMENT_MAX, 'Back comment')
  if (bc) {
    errors.backComment = bc
  }

  return errors
}

export function hasCardFieldErrors(errors: CardFormFieldErrors): boolean {
  return Object.keys(errors).length > 0
}
