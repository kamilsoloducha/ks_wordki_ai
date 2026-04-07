/** Jedna karta z importu — zgodnie z `POST /api/cards/cards`. */
export type ImportGroupRow = {
  frontLabel: string
  backLabel: string
  frontExample: string
  backExample: string
}

const LABEL_MAX = 200
const EXAMPLE_MAX = 1000

function splitTwoColumns(line: string): [string, string] | null {
  const trimmed = line.trim()
  if (!trimmed) {
    return null
  }

  if (trimmed.includes('\t')) {
    return null
  }

  const semi = trimmed.indexOf(';')
  if (semi !== -1) {
    const a = trimmed.slice(0, semi).trim()
    const b = trimmed.slice(semi + 1).trim()
    if (a && b) {
      return [a, b]
    }
    return null
  }

  const comma = trimmed.indexOf(',')
  if (comma !== -1) {
    const a = trimmed.slice(0, comma).trim()
    const b = trimmed.slice(comma + 1).trim()
    if (a && b) {
      return [a, b]
    }
    return null
  }

  return null
}

/** Wiersz z tabulatorami: 2 = tylko etykiety, 3 = front/back + front example, 4+ = oba przykłady. */
function parseTabSeparatedLine(line: string): ImportGroupRow | null {
  const trimmed = line.trim()
  if (!trimmed.includes('\t')) {
    return null
  }
  const parts = trimmed.split('\t').map((p) => p.trim())
  if (parts.length < 2) {
    return null
  }
  if (parts.length === 2) {
    return {
      frontLabel: parts[0],
      backLabel: parts[1],
      frontExample: '',
      backExample: '',
    }
  }
  if (parts.length === 3) {
    return {
      frontLabel: parts[0],
      backLabel: parts[1],
      frontExample: parts[2],
      backExample: '',
    }
  }
  return {
    frontLabel: parts[0],
    backLabel: parts[1],
    frontExample: parts[2] ?? '',
    backExample: parts[3] ?? '',
  }
}

function parseDataLine(line: string): ImportGroupRow | null {
  const tabbed = parseTabSeparatedLine(line)
  if (tabbed) {
    return tabbed
  }
  const pair = splitTwoColumns(line)
  if (!pair) {
    return null
  }
  return {
    frontLabel: pair[0],
    backLabel: pair[1],
    frontExample: '',
    backExample: '',
  }
}

function looksLikeHeader(front: string, back: string): boolean {
  const f = front.trim().toLowerCase()
  const b = back.trim().toLowerCase()
  return (
    (f === 'front' && b === 'back') ||
    (f === 'przód' && b === 'tył') ||
    (f === 'label' && b === 'label')
  )
}

export type ParseImportGroupCsvResult =
  | { ok: true; rows: ImportGroupRow[] }
  | { ok: false; error: string }

/** Wynik podglądu: pusty tekst nie jest błędem parsowania. */
export type ImportGroupPreview =
  | { kind: 'empty' }
  | { kind: 'ok'; rows: ImportGroupRow[] }
  | { kind: 'error'; error: string }

export function getImportGroupPreview(text: string): ImportGroupPreview {
  if (!text.trim()) {
    return { kind: 'empty' }
  }
  const r = parseImportGroupCsv(text)
  if (r.ok) {
    return { kind: 'ok', rows: r.rows }
  }
  return { kind: 'error', error: r.error }
}

/** Maks. liczba wierszy (zabezpieczenie przed wklejeniem zbyt dużego pliku). */
export const IMPORT_GROUP_MAX_ROWS = 3000

/**
 * Parsuje tekst: jedna karta na linię.
 * - **Tab**: 2 kolumny (front, back), 3 (front, back, front example) lub 4+ (front, back, oba przykłady).
 * - Bez tabulatora: jak wcześniej — średnik lub pierwszy przecinek — tylko dwie etykiety (bez przykładów).
 */
export function parseImportGroupCsv(text: string): ParseImportGroupCsvResult {
  const lines = text.split(/\r?\n/)
  const rawRows: ImportGroupRow[] = []
  let skippedHeader = false

  for (let i = 0; i < lines.length; i++) {
    const row = parseDataLine(lines[i])
    if (!row) {
      continue
    }

    if (!skippedHeader && rawRows.length === 0 && looksLikeHeader(row.frontLabel, row.backLabel)) {
      skippedHeader = true
      continue
    }

    if (row.frontLabel.length > LABEL_MAX || row.backLabel.length > LABEL_MAX) {
      return {
        ok: false,
        error: `Line ${i + 1}: each label must be at most ${LABEL_MAX} characters.`,
      }
    }

    if (
      row.frontExample.length > EXAMPLE_MAX ||
      row.backExample.length > EXAMPLE_MAX
    ) {
      return {
        ok: false,
        error: `Line ${i + 1}: example text must be at most ${EXAMPLE_MAX} characters.`,
      }
    }

    rawRows.push(row)
  }

  if (rawRows.length === 0) {
    return {
      ok: false,
      error:
        'No card rows found. Use tab-separated columns: front and back; add optional third (front example) and fourth (back example) column. Without tabs you can use two columns with semicolon or comma.',
    }
  }

  if (rawRows.length > IMPORT_GROUP_MAX_ROWS) {
    return {
      ok: false,
      error: `Too many rows (max ${IMPORT_GROUP_MAX_ROWS}). Split into multiple imports.`,
    }
  }

  return { ok: true, rows: rawRows }
}
