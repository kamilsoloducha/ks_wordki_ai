export function normalizeSearchQuery(raw: string): string {
  return raw.trim().toLowerCase()
}

export function filterGroupsByName<T extends { name: string }>(
  items: readonly T[],
  rawQuery: string,
): T[] {
  const q = normalizeSearchQuery(rawQuery)
  if (!q) return [...items]
  return items.filter((g) => g.name.toLowerCase().includes(q))
}

export function filterCardsByLabels<
  T extends { front: { label: string }; back: { label: string } },
>(items: readonly T[], rawQuery: string): T[] {
  const q = normalizeSearchQuery(rawQuery)
  if (!q) return [...items]
  return items.filter(
    (c) =>
      c.front.label.toLowerCase().includes(q) ||
      c.back.label.toLowerCase().includes(q),
  )
}
