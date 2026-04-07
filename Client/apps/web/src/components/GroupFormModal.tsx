import { useEffect, useId, useMemo, useRef, useState } from 'react'
import {
  WordkiApiError,
  type UserCardGroup,
} from '@wordki/shared'
import { ComboboxInputField } from './ComboboxInputField'
import { useWordkiBackend } from '../hooks/useWordkiBackend'
import {
  hasGroupFieldErrors,
  validateGroupForm,
  type GroupFormFieldErrors,
  type GroupFormValues,
} from '../lib/groupFormValidation'
import { getSideTypeSuggestionsFromGroups } from '../lib/groupSideTypeSuggestions'
import { hasFieldErrors } from '../lib/registerValidation'
import './GroupFormModal.css'

export type GroupFormModalProps = {
  open: boolean
  mode: 'create' | 'edit'
  /** Required when `mode === 'edit'`. */
  group: UserCardGroup | null
  /** All user groups (for combobox suggestions from other groups). */
  allGroups: readonly UserCardGroup[]
  userId: string
  onClose: () => void
  onSaved: () => void
}

const emptyValues: GroupFormValues = {
  name: '',
  frontSideType: '',
  backSideType: '',
}

export function GroupFormModal({
  open,
  mode,
  group,
  allGroups,
  userId,
  onClose,
  onSaved,
}: GroupFormModalProps) {
  const api = useWordkiBackend()
  const titleId = useId()
  const firstFieldRef = useRef<HTMLInputElement>(null)

  const sideTypeSuggestions = useMemo(
    () =>
      getSideTypeSuggestionsFromGroups(
        allGroups,
        mode === 'edit' && group ? group.id : undefined,
      ),
    [allGroups, mode, group],
  )

  const [values, setValues] = useState<GroupFormValues>(emptyValues)
  const [fieldErrors, setFieldErrors] = useState<GroupFormFieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!open) {
      return
    }
    setFieldErrors({})
    setFormError(null)
    if (mode === 'edit' && group) {
      setValues({
        name: group.name,
        frontSideType: group.frontSideType,
        backSideType: group.backSideType,
      })
    } else {
      setValues(emptyValues)
    }
    queueMicrotask(() => firstFieldRef.current?.focus())
  }, [open, mode, group])

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

  function updateField<K extends keyof GroupFormValues>(
    key: K,
    value: GroupFormValues[K],
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

    const nextErrors = validateGroupForm(values)
    setFieldErrors(nextErrors)
    if (hasGroupFieldErrors(nextErrors)) {
      return
    }

    if (mode === 'edit' && !group) {
      return
    }

    const payload = {
      userId,
      name: values.name.trim(),
      frontSideType: values.frontSideType.trim(),
      backSideType: values.backSideType.trim(),
    }

    setSubmitting(true)
    try {
      if (mode === 'create') {
        await api.createCardGroup(payload)
      } else {
        await api.updateCardGroup(group!.id, payload)
      }
      onSaved()
      onClose()
    } catch (err) {
      if (err instanceof WordkiApiError) {
        const apiFieldMap: GroupFormFieldErrors = {}
        for (const item of err.errors) {
          const f = item.field?.toLowerCase()
          if (f === 'name') apiFieldMap.name = item.message
          else if (f === 'frontsidetype') apiFieldMap.frontSideType = item.message
          else if (f === 'backsidetype') apiFieldMap.backSideType = item.message
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

  const title = mode === 'create' ? 'New group' : 'Edit group'

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
        className="group-modal"
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
          <div className="group-modal__field">
            <label htmlFor="group-modal-name">Name</label>
            <input
              ref={firstFieldRef}
              id="group-modal-name"
              name="name"
              type="text"
              autoComplete="off"
              value={values.name}
              onChange={(e) => updateField('name', e.target.value)}
              className={fieldErrors.name ? 'has-error' : ''}
              aria-invalid={!!fieldErrors.name}
              aria-describedby={
                fieldErrors.name ? 'group-modal-name-err' : undefined
              }
            />
            {fieldErrors.name && (
              <span id="group-modal-name-err" className="group-modal__error" role="alert">
                {fieldErrors.name}
              </span>
            )}
          </div>

          <ComboboxInputField
            id="group-modal-front"
            name="frontSideType"
            label="Front side type"
            hint="Pick a suggestion or type your own. Suggestions use types from your other groups."
            value={values.frontSideType}
            onValueChange={(v) => updateField('frontSideType', v)}
            suggestions={sideTypeSuggestions}
            error={fieldErrors.frontSideType}
            suggestionKeyPrefix="front"
          />

          <ComboboxInputField
            id="group-modal-back"
            name="backSideType"
            label="Back side type"
            hint="Pick a suggestion or type your own. Suggestions use types from your other groups."
            value={values.backSideType}
            onValueChange={(v) => updateField('backSideType', v)}
            suggestions={sideTypeSuggestions}
            error={fieldErrors.backSideType}
            suggestionKeyPrefix="back"
          />

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
                  ? 'Create group'
                  : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
