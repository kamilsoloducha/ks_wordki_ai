import {
  useEffect,
  useId,
  useRef,
  useState,
  type Ref,
} from 'react'
import { WordkiApiError, type GroupCard } from '@wordki/shared'
import { useWordkiBackend } from '../hooks/useWordkiBackend'
import {
  hasCardFieldErrors,
  validateCardForm,
  type CardFormFieldErrors,
  type CardFormValues,
} from '../lib/cardFormValidation'
import { hasFieldErrors } from '../lib/registerValidation'
import './GroupFormModal.css'

export type CardFormModalProps = {
  open: boolean
  mode: 'create' | 'edit'
  groupId: number
  card: GroupCard | null
  userId: string
  /** Front side type from group settings (e.g. language label), shown above front fields. */
  frontSideType?: string
  /** Back side type from group settings, shown above back fields. */
  backSideType?: string
  onClose: () => void
  onSaved: () => void
}

const emptyValues: CardFormValues = {
  frontLabel: '',
  backLabel: '',
  frontExample: '',
  frontComment: '',
  backExample: '',
  backComment: '',
}

function applyApiFieldError(
  field: string | null | undefined,
  message: string,
  target: CardFormFieldErrors,
): void {
  const f = (field ?? '').toLowerCase()
  if (f === 'frontlabel' || f === 'front.label') {
    target.frontLabel = message
  } else if (f === 'backlabel' || f === 'back.label') {
    target.backLabel = message
  } else if (f === 'frontexample' || f === 'front.example') {
    target.frontExample = message
  } else if (f === 'frontcomment' || f === 'front.comment') {
    target.frontComment = message
  } else if (f === 'backexample' || f === 'back.example') {
    target.backExample = message
  } else if (f === 'backcomment' || f === 'back.comment') {
    target.backComment = message
  }
}

export function CardFormModal({
  open,
  mode,
  groupId,
  card,
  userId,
  frontSideType,
  backSideType,
  onClose,
  onSaved,
}: CardFormModalProps) {
  const api = useWordkiBackend()
  const titleId = useId()
  const firstFieldRef = useRef<HTMLInputElement>(null)

  const [values, setValues] = useState<CardFormValues>(emptyValues)
  const [fieldErrors, setFieldErrors] = useState<CardFormFieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!open) {
      return
    }
    setFieldErrors({})
    setFormError(null)
    if (mode === 'edit' && card) {
      setValues({
        frontLabel: card.front.label,
        backLabel: card.back.label,
        frontExample: card.front.example,
        frontComment: card.front.comment,
        backExample: card.back.example,
        backComment: card.back.comment,
      })
    } else {
      setValues(emptyValues)
    }
    queueMicrotask(() => firstFieldRef.current?.focus())
  }, [open, mode, card])

  useEffect(() => {
    if (!open) {
      return
    }
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault()
        onClose()
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  useEffect(() => {
    if (!open) {
      return
    }
    const prev = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = prev
    }
  }, [open])

  function updateField<K extends keyof CardFormValues>(
    key: K,
    value: CardFormValues[K],
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

    const nextErrors = validateCardForm(values)
    setFieldErrors(nextErrors)
    if (hasCardFieldErrors(nextErrors)) {
      return
    }

    if (mode === 'edit' && !card) {
      return
    }

    setSubmitting(true)
    try {
      if (mode === 'create') {
        await api.createCard({
          userId,
          groupId,
          frontLabel: values.frontLabel.trim(),
          backLabel: values.backLabel.trim(),
          frontExample: values.frontExample.trim() || undefined,
          frontComment: values.frontComment.trim() || undefined,
          backExample: values.backExample.trim() || undefined,
          backComment: values.backComment.trim() || undefined,
        })
      } else {
        await api.updateCard(card!.id, {
          userId,
          front: {
            label: values.frontLabel.trim(),
            example: values.frontExample.trim(),
            comment: values.frontComment.trim(),
          },
          back: {
            label: values.backLabel.trim(),
            example: values.backExample.trim(),
            comment: values.backComment.trim(),
          },
        })
      }
      onSaved()
      if (mode === 'create') {
        setValues({ ...emptyValues })
        setFieldErrors({})
        setFormError(null)
        queueMicrotask(() => firstFieldRef.current?.focus())
      } else {
        onClose()
      }
    } catch (err) {
      if (err instanceof WordkiApiError) {
        const apiFieldMap: CardFormFieldErrors = {}
        for (const item of err.errors) {
          applyApiFieldError(item.field, item.message, apiFieldMap)
        }
        const mapped = hasFieldErrors(apiFieldMap)
        setFieldErrors(apiFieldMap)
        setFormError(
          mapped
            ? null
            : err.errors[0]?.message ??
                `Request failed (${err.status || 'error'}).`,
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

  if (!open) {
    return null
  }

  const title = mode === 'create' ? 'New card' : 'Edit card'

  function field(
    id: string,
    name: keyof CardFormValues,
    label: string,
    multiline?: boolean,
    inputRef?: Ref<HTMLInputElement>,
    excludeFromTabOrder?: boolean,
    hintDescribedBy?: string,
  ) {
    const tabProps = excludeFromTabOrder ? { tabIndex: -1 as const } : {}
    const describedBy = [
      hintDescribedBy,
      fieldErrors[name] ? `${id}-err` : null,
    ]
      .filter(Boolean)
      .join(' ') || undefined
    return (
      <div className="group-modal__field">
        <label htmlFor={id}>{label}</label>
        {multiline ? (
          <textarea
            id={id}
            name={name}
            rows={2}
            value={values[name]}
            onChange={(e) => updateField(name, e.target.value)}
            className={`group-modal__textarea${fieldErrors[name] ? ' has-error' : ''}`}
            aria-invalid={!!fieldErrors[name]}
            aria-describedby={describedBy}
            {...tabProps}
          />
        ) : (
          <input
            ref={inputRef}
            id={id}
            name={name}
            type="text"
            autoComplete="off"
            value={values[name]}
            onChange={(e) => updateField(name, e.target.value)}
            className={fieldErrors[name] ? 'has-error' : ''}
            aria-invalid={!!fieldErrors[name]}
            aria-describedby={describedBy}
            {...tabProps}
          />
        )}
        {fieldErrors[name] && (
          <span id={`${id}-err`} className="group-modal__error" role="alert">
            {fieldErrors[name]}
          </span>
        )}
      </div>
    )
  }

  return (
    <div
      className="group-modal-root"
      role="presentation"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        className="group-modal group-modal--wide"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <div className="group-modal__header">
          <h2 id={titleId} className="group-modal__title">
            {title}
          </h2>
          <button
            type="button"
            className="group-modal__close"
            onClick={onClose}
            aria-label="Close"
          >
            ×
          </button>
        </div>

        {formError && (
          <div className="group-modal__banner" role="alert">
            {formError}
          </div>
        )}

        <form
          className="group-modal__form"
          onSubmit={(e) => void handleSubmit(e)}
          noValidate
        >
          <div className="card-form-modal__grid">
            <fieldset className="card-form-modal__side card-form-modal__side--front">
              <legend className="card-form-modal__legend">Front</legend>
              {frontSideType?.trim() ? (
                <p
                  className="card-form-modal__side-hint"
                  id="card-modal-front-side-hint"
                >
                  Group side type:{' '}
                  <span className="card-form-modal__side-type">
                    {frontSideType.trim()}
                  </span>
                </p>
              ) : null}
              {field(
                'card-modal-fl',
                'frontLabel',
                'Label',
                false,
                firstFieldRef,
                undefined,
                frontSideType?.trim()
                  ? 'card-modal-front-side-hint'
                  : undefined,
              )}
              {field('card-modal-fe', 'frontExample', 'Example', true)}
              {field('card-modal-fc', 'frontComment', 'Comment', true, undefined, true)}
            </fieldset>
            <fieldset className="card-form-modal__side card-form-modal__side--back">
              <legend className="card-form-modal__legend">Back</legend>
              {backSideType?.trim() ? (
                <p
                  className="card-form-modal__side-hint"
                  id="card-modal-back-side-hint"
                >
                  Group side type:{' '}
                  <span className="card-form-modal__side-type">
                    {backSideType.trim()}
                  </span>
                </p>
              ) : null}
              {field(
                'card-modal-bl',
                'backLabel',
                'Label',
                false,
                undefined,
                undefined,
                backSideType?.trim()
                  ? 'card-modal-back-side-hint'
                  : undefined,
              )}
              {field('card-modal-be', 'backExample', 'Example', true)}
              {field('card-modal-bc', 'backComment', 'Comment', true, undefined, true)}
            </fieldset>
          </div>

          <div className="group-modal__actions">
            <button
              type="button"
              className="group-modal__btn group-modal__btn--ghost"
              onClick={onClose}
              disabled={submitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="group-modal__btn group-modal__btn--primary"
              disabled={submitting}
            >
              {submitting
                ? mode === 'create'
                  ? 'Creating…'
                  : 'Saving…'
                : mode === 'create'
                  ? 'Add card'
                  : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
