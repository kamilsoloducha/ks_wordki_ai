import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { WordkiApiError } from '@wordki/shared'
import type { UserCardGroup } from '@wordki/shared'
import { useAuth } from '../auth/AuthContext'
import { useWordkiBackend } from '../hooks/useWordkiBackend'
import {
  getImportGroupPreview,
  IMPORT_GROUP_MAX_ROWS,
  parseImportGroupCsv,
} from '../lib/importGroupCsv'
import {
  hasGroupFieldErrors,
  validateGroupForm,
  type GroupFormFieldErrors,
  type GroupFormValues,
} from '../lib/groupFormValidation'
import '../components/GroupFormModal.css'
import './ImportGroupPage.css'
import './RegisterPage.css'

const emptyGroup: GroupFormValues = {
  name: '',
  frontSideType: '',
  backSideType: '',
}

export function ImportGroupPage() {
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()
  const api = useWordkiBackend()

  const [group, setGroup] = useState<GroupFormValues>(emptyGroup)
  const [csvText, setCsvText] = useState('')
  const [fieldErrors, setFieldErrors] = useState<GroupFormFieldErrors>({})
  const [parseError, setParseError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [progress, setProgress] = useState<{ current: number; total: number } | null>(
    null,
  )

  const preview = useMemo(() => getImportGroupPreview(csvText), [csvText])

  const previewHasExamples = useMemo(() => {
    if (preview.kind !== 'ok') {
      return false
    }
    return preview.rows.some(
      (r) => r.frontExample.trim() !== '' || r.backExample.trim() !== '',
    )
  }, [preview])

  const frontCol =
    group.frontSideType.trim() || 'Front'
  const backCol =
    group.backSideType.trim() || 'Back'

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: '/groups/import' } })
    }
  }, [isAuthenticated, navigate])

  function updateGroup<K extends keyof GroupFormValues>(key: K, value: string) {
    setGroup((g) => ({ ...g, [key]: value }))
    setFieldErrors((e) => {
      const n = { ...e }
      delete n[key]
      return n
    })
    setFormError(null)
    setParseError(null)
  }

  function handleImportTextareaKeyDown(
    e: React.KeyboardEvent<HTMLTextAreaElement>,
  ) {
    if (e.key !== 'Tab' || e.shiftKey) {
      return
    }
    e.preventDefault()
    const el = e.currentTarget
    const start = el.selectionStart ?? 0
    const end = el.selectionEnd ?? 0
    const next = csvText.slice(0, start) + '\t' + csvText.slice(end)
    setCsvText(next)
    setParseError(null)
    setFormError(null)
    const pos = start + 1
    setTimeout(() => {
      el.focus()
      try {
        el.setSelectionRange(pos, pos)
      } catch {
        /* ignore if unmounted */
      }
    }, 0)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError(null)
    setParseError(null)

    const ve = validateGroupForm(group)
    setFieldErrors(ve)
    if (hasGroupFieldErrors(ve)) {
      return
    }

    const parsed = parseImportGroupCsv(csvText)
    if (!parsed.ok) {
      setParseError(parsed.error)
      return
    }

    if (!user?.id) {
      return
    }

    setSubmitting(true)
    setProgress({ current: 0, total: parsed.rows.length })

    try {
      const created = await api.createCardGroup({
        userId: user.id,
        name: group.name.trim(),
        frontSideType: group.frontSideType.trim(),
        backSideType: group.backSideType.trim(),
      })

      const groupId = created.id

      for (let i = 0; i < parsed.rows.length; i++) {
        const row = parsed.rows[i]
        setProgress({ current: i + 1, total: parsed.rows.length })
        try {
          const fe = row.frontExample.trim()
          const be = row.backExample.trim()
          await api.createCard({
            userId: user.id,
            groupId,
            frontLabel: row.frontLabel,
            backLabel: row.backLabel,
            frontExample: fe || undefined,
            backExample: be || undefined,
          })
        } catch (err) {
          const rowNum = i + 1
          if (err instanceof WordkiApiError) {
            const msg =
              err.errors[0]?.message ??
              `Request failed (${err.status ?? 'error'}).`
            const preview =
              row.frontLabel.length > 36
                ? `${row.frontLabel.slice(0, 36)}…`
                : row.frontLabel
            throw new Error(`Card row ${rowNum} (${preview}): ${msg}`)
          }
          throw new Error(
            `Card row ${rowNum}: ${err instanceof Error ? err.message : 'Unknown error'}`,
          )
        }
      }

      const navState: UserCardGroup = {
        id: created.id,
        name: created.name,
        frontSideType: created.frontSideType,
        backSideType: created.backSideType,
        cardCount: parsed.rows.length,
      }

      navigate(`/groups/${created.id}`, { state: { group: navState } })
    } catch (err) {
      setFormError(
        err instanceof Error ? err.message : 'Import failed.',
      )
    } finally {
      setSubmitting(false)
      setProgress(null)
    }
  }

  if (!isAuthenticated || !user) {
    return null
  }

  return (
    <main className="register-page import-group-page">
      <div
        className={
          'register-page__card import-group-page__inner' +
          (previewHasExamples ? ' import-group-page__inner--wide' : '')
        }
      >
        <nav className="import-group-page__back-nav" aria-label="Navigation">
          <Link to="/groups" className="register-page__back">
            ← Back to groups
          </Link>
        </nav>

        <h1 className="register-page__title">Import group from CSV</h1>
        <p className="register-page__lead import-group-page__lead">
          Create a new group and add cards from pasted text. Each line is one card. With{' '}
          <strong>tabs</strong> (e.g. from Excel): two columns for front and back; add a
          third column for a <strong>front example</strong> and a fourth for a{' '}
          <strong>back example</strong>. Without tabs you can use two columns with{' '}
          <strong>semicolon</strong> or the first <strong>comma</strong> — labels only, no
          examples.
        </p>

        {(formError || parseError) && (
          <div className="register-page__banner" role="alert">
            {formError ?? parseError}
          </div>
        )}

        <form
          className="import-group-page__form"
          onSubmit={(e) => void handleSubmit(e)}
          noValidate
        >
          <div className="import-group-page__fields">
            <div className="group-modal__field">
              <label htmlFor="import-group-name">Group name</label>
              <input
                id="import-group-name"
                type="text"
                autoComplete="off"
                value={group.name}
                onChange={(e) => updateGroup('name', e.target.value)}
                className={fieldErrors.name ? 'has-error' : ''}
                aria-invalid={!!fieldErrors.name}
              />
              {fieldErrors.name && (
                <span className="group-modal__error" role="alert">
                  {fieldErrors.name}
                </span>
              )}
            </div>
            <div className="import-group-page__side-types">
              <div className="group-modal__field">
                <label htmlFor="import-front-type">Front side type</label>
                <input
                  id="import-front-type"
                  type="text"
                  autoComplete="off"
                  placeholder="e.g. EN"
                  value={group.frontSideType}
                  onChange={(e) => updateGroup('frontSideType', e.target.value)}
                  className={fieldErrors.frontSideType ? 'has-error' : ''}
                  aria-invalid={!!fieldErrors.frontSideType}
                />
                {fieldErrors.frontSideType && (
                  <span className="group-modal__error" role="alert">
                    {fieldErrors.frontSideType}
                  </span>
                )}
              </div>
              <div className="group-modal__field">
                <label htmlFor="import-back-type">Back side type</label>
                <input
                  id="import-back-type"
                  type="text"
                  autoComplete="off"
                  placeholder="e.g. PL"
                  value={group.backSideType}
                  onChange={(e) => updateGroup('backSideType', e.target.value)}
                  className={fieldErrors.backSideType ? 'has-error' : ''}
                  aria-invalid={!!fieldErrors.backSideType}
                />
                {fieldErrors.backSideType && (
                  <span className="group-modal__error" role="alert">
                    {fieldErrors.backSideType}
                  </span>
                )}
              </div>
            </div>
          </div>

          <div className="group-modal__field import-group-page__csv-field">
            <label htmlFor="import-csv-body">Cards (CSV / table)</label>
            <textarea
              id="import-csv-body"
              className="group-modal__textarea import-group-page__textarea"
              rows={14}
              value={csvText}
              onChange={(e) => {
                setCsvText(e.target.value)
                setParseError(null)
                setFormError(null)
              }}
              onKeyDown={handleImportTextareaKeyDown}
              placeholder={
                'hello\thallo\tShe said hello.\tPowiedziała cześć.\n' +
                'thanks\tdanke\n' +
                '\n' +
                'Optional header: Front<Tab>Back<Tab>Front example<Tab>Back example'
              }
              spellCheck={false}
            />
            <p className="import-group-page__hint">
              Up to {IMPORT_GROUP_MAX_ROWS} cards per import. Tab-separated: 2 columns
              (labels), 3 (labels + front example), or 4 (both examples). Press{' '}
              <kbd className="import-group-page__kbd">Tab</kbd> to insert a tab character;
              use <kbd className="import-group-page__kbd">Shift</kbd> +{' '}
              <kbd className="import-group-page__kbd">Tab</kbd> to move focus out. Optional
              first line <code>Front</code> + tab + <code>Back</code> (and example headers)
              is skipped.
            </p>
          </div>

          <div
            className="import-group-page__preview"
            aria-label="Live import preview"
          >
            <h2 className="import-group-page__preview-title">Preview</h2>
            {preview.kind === 'empty' && (
              <p className="import-group-page__preview-empty" role="status">
                Paste data above — a live preview of cards will appear here.
              </p>
            )}
            {preview.kind === 'error' && (
              <div
                className="import-group-page__preview-error"
                role="status"
              >
                {preview.error}
              </div>
            )}
            {preview.kind === 'ok' && (
              <>
                <p className="import-group-page__preview-count" aria-live="polite">
                  {preview.rows.length === 1
                    ? '1 card'
                    : `${preview.rows.length} cards`}{' '}
                  ready to import
                </p>
                <div className="import-group-page__preview-scroll">
                  <table className="import-group-page__preview-table">
                    <thead>
                      <tr>
                        <th scope="col">#</th>
                        <th scope="col">{frontCol}</th>
                        <th scope="col">{backCol}</th>
                        {previewHasExamples && (
                          <>
                            <th scope="col">Front example</th>
                            <th scope="col">Back example</th>
                          </>
                        )}
                      </tr>
                    </thead>
                    <tbody>
                      {preview.rows.map((row, i) => (
                        <tr key={`preview-${i}`}>
                          <td className="import-group-page__preview-num">
                            {i + 1}
                          </td>
                          <td className="import-group-page__preview-cell">
                            {row.frontLabel}
                          </td>
                          <td className="import-group-page__preview-cell">
                            {row.backLabel}
                          </td>
                          {previewHasExamples && (
                            <>
                              <td className="import-group-page__preview-cell import-group-page__preview-cell--muted">
                                {row.frontExample || '—'}
                              </td>
                              <td className="import-group-page__preview-cell import-group-page__preview-cell--muted">
                                {row.backExample || '—'}
                              </td>
                            </>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>

          {progress && (
            <p className="import-group-page__progress" aria-live="polite">
              Importing cards… {progress.current} / {progress.total}
            </p>
          )}

          <div className="group-modal__actions import-group-page__actions">
            <Link
              to="/groups"
              className="group-modal__btn group-modal__btn--ghost import-group-page__cancel"
            >
              Cancel
            </Link>
            <button
              type="submit"
              className="group-modal__btn group-modal__btn--primary"
              disabled={submitting}
            >
              {submitting ? 'Importing…' : 'Create group and import'}
            </button>
          </div>
        </form>
      </div>
    </main>
  )
}
