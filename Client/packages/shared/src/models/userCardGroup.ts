/**
 * Odpowiada `UserCardGroupDto` z BFF (`GET /api/cards/groups?userId=`).
 * JSON z API jest w camelCase (np. frontSideType).
 */
export interface UserCardGroup {
  readonly id: number;
  readonly name: string;
  readonly frontSideType: string;
  readonly backSideType: string;
  readonly cardCount: number;
}

export function parseUserCardGroup(value: unknown): UserCardGroup | null {
  if (value === null || typeof value !== 'object') {
    return null;
  }

  const o = value as Record<string, unknown>;
  if (
    typeof o.id !== 'number' ||
    typeof o.name !== 'string' ||
    typeof o.frontSideType !== 'string' ||
    typeof o.backSideType !== 'string' ||
    typeof o.cardCount !== 'number'
  ) {
    return null;
  }

  return {
    id: o.id,
    name: o.name,
    frontSideType: o.frontSideType,
    backSideType: o.backSideType,
    cardCount: o.cardCount,
  };
}

export function parseUserCardGroupList(value: unknown): UserCardGroup[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.map(parseUserCardGroup).filter((x): x is UserCardGroup => x !== null);
}
