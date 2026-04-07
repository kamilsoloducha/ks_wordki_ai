import type { UserCardGroup } from '@wordki/shared'

/**
 * Unique non-empty side types from groups, optionally excluding one group (e.g. the one being edited).
 */
export function getSideTypeSuggestionsFromGroups(
  groups: readonly UserCardGroup[],
  excludeGroupId?: number,
): string[] {
  const set = new Set<string>()
  for (const g of groups) {
    if (excludeGroupId !== undefined && g.id === excludeGroupId) {
      continue
    }
    const front = g.frontSideType.trim()
    const back = g.backSideType.trim()
    if (front) {
      set.add(front)
    }
    if (back) {
      set.add(back)
    }
  }
  return [...set].sort((a, b) =>
    a.localeCompare(b, undefined, { sensitivity: 'base' }),
  )
}
