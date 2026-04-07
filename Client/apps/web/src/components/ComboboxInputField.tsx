import {
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from 'react'
import './ComboboxInputField.css'

export type ComboboxInputFieldProps = {
  id: string
  name: string
  label: string
  /** Shown below the label; linked for screen readers. */
  hint?: string
  value: string
  onValueChange: (value: string) => void
  /** Values shown in the suggestion list (free text still allowed). */
  suggestions: readonly string[]
  error?: string
  /** Disambiguates React keys for list items (e.g. `front` / `back`). */
  suggestionKeyPrefix: string
}

/**
 * Text input with a custom suggestion list (same width as the input).
 * Native `<datalist>` cannot be styled for width in most browsers.
 */
export function ComboboxInputField({
  id,
  name,
  label,
  hint,
  value,
  onValueChange,
  suggestions,
  error,
  suggestionKeyPrefix,
}: ComboboxInputFieldProps) {
  const listboxId = useId()
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-err` : undefined
  const describedBy =
    [hintId, errorId].filter(Boolean).join(' ') || undefined

  const inputRef = useRef<HTMLInputElement>(null)
  const [open, setOpen] = useState(false)
  const [highlighted, setHighlighted] = useState(0)

  const filtered = useMemo(() => {
    const q = value.trim().toLowerCase()
    if (!q) {
      return [...suggestions]
    }
    return suggestions.filter((s) => s.toLowerCase().includes(q))
  }, [suggestions, value])

  const showList = open && filtered.length > 0

  useEffect(() => {
    setHighlighted((h) =>
      filtered.length === 0 ? 0 : Math.min(h, filtered.length - 1),
    )
  }, [filtered])

  const selectOption = (next: string) => {
    onValueChange(next)
    setOpen(false)
    inputRef.current?.blur()
  }

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (!showList && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
      if (suggestions.length === 0) {
        return
      }
      setOpen(true)
      setHighlighted(0)
      e.preventDefault()
      return
    }
    if (!showList) {
      return
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setHighlighted((i) => Math.min(i + 1, filtered.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setHighlighted((i) => Math.max(i - 1, 0))
    } else if (e.key === 'Enter' && filtered.length > 0) {
      e.preventDefault()
      selectOption(filtered[highlighted]!)
    } else if (e.key === 'Escape') {
      setOpen(false)
    }
  }

  return (
    <div className="group-modal__field">
      <label htmlFor={id}>{label}</label>
      {hint ? (
        <p className="group-modal__field-hint" id={hintId}>
          {hint}
        </p>
      ) : null}
      <div className="combobox-input-field">
        <input
          ref={inputRef}
          id={id}
          name={name}
          type="text"
          autoComplete="off"
          value={value}
          onChange={(e) => onValueChange(e.target.value)}
          onFocus={() => {
            if (suggestions.length > 0) {
              setOpen(true)
            }
          }}
          onBlur={() => {
            window.setTimeout(() => setOpen(false), 120)
          }}
          onKeyDown={handleKeyDown}
          className={error ? 'has-error' : ''}
          aria-invalid={!!error}
          aria-describedby={describedBy}
          role="combobox"
          aria-expanded={showList}
          aria-controls={listboxId}
          aria-autocomplete="list"
        />
        {showList ? (
          <ul
            id={listboxId}
            className="combobox-input-field__list"
            role="listbox"
          >
            {filtered.map((s, i) => (
              <li
                key={`${suggestionKeyPrefix}-${s}`}
                role="option"
                aria-selected={i === highlighted}
                className={
                  'combobox-input-field__option' +
                  (i === highlighted ? ' is-highlighted' : '')
                }
                onMouseDown={(e) => {
                  e.preventDefault()
                  selectOption(s)
                }}
                onMouseEnter={() => setHighlighted(i)}
              >
                {s}
              </li>
            ))}
          </ul>
        ) : null}
      </div>
      {error ? (
        <span id={errorId} className="group-modal__error" role="alert">
          {error}
        </span>
      ) : null}
    </div>
  )
}
