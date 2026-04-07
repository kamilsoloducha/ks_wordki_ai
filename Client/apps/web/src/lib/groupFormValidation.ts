export type GroupFormValues = {
  name: string
  frontSideType: string
  backSideType: string
}

export type GroupFormFieldErrors = Partial<
  Record<keyof GroupFormValues, string>
>

/** Mirrors cards module `CreateCardGroupCommandValidator` limits. */
const NAME_MAX = 200
const SIDE_MAX = 100

export function validateGroupForm(
  values: GroupFormValues,
): GroupFormFieldErrors {
  const errors: GroupFormFieldErrors = {}

  const name = values.name.trim()
  if (!name) {
    errors.name = 'Name is required.'
  } else if (name.length > NAME_MAX) {
    errors.name = `Name cannot exceed ${NAME_MAX} characters.`
  }

  const front = values.frontSideType.trim()
  if (!front) {
    errors.frontSideType = 'Front side type is required.'
  } else if (front.length > SIDE_MAX) {
    errors.frontSideType = `Front side type cannot exceed ${SIDE_MAX} characters.`
  }

  const back = values.backSideType.trim()
  if (!back) {
    errors.backSideType = 'Back side type is required.'
  } else if (back.length > SIDE_MAX) {
    errors.backSideType = `Back side type cannot exceed ${SIDE_MAX} characters.`
  }

  return errors
}

export function hasGroupFieldErrors(
  errors: GroupFormFieldErrors,
): boolean {
  return Object.keys(errors).length > 0
}
