/**
 * `CardDto` z BFF — lista kart w grupie (`GET /api/cards/groups/{id}/cards`).
 */
export interface CardSide {
  readonly label: string;
  readonly example: string;
  readonly comment: string;
}

export interface GroupCard {
  readonly id: number;
  readonly groupId: number;
  readonly front: CardSide;
  readonly back: CardSide;
  /**
   * Id wyniku SRS dla strony pytania (kierunek lekcji), jeśli API je zwraca
   * (np. `GET /api/cards/due-today` z `questionSideType` / `answerSideType`).
   */
  readonly questionResultId?: number;
}

function parseCardSide(value: unknown): CardSide | null {
  if (value === null || typeof value !== 'object') {
    return null;
  }
  const o = value as Record<string, unknown>;
  if (
    typeof o.label !== 'string' ||
    typeof o.example !== 'string' ||
    typeof o.comment !== 'string'
  ) {
    return null;
  }
  return { label: o.label, example: o.example, comment: o.comment };
}

export function parseGroupCard(value: unknown): GroupCard | null {
  if (value === null || typeof value !== 'object') {
    return null;
  }
  const o = value as Record<string, unknown>;
  if (typeof o.id !== 'number' || typeof o.groupId !== 'number') {
    return null;
  }
  const front = parseCardSide(o.front);
  const back = parseCardSide(o.back);
  if (!front || !back) {
    return null;
  }
  const questionResultId = o.questionResultId;
  return {
    id: o.id,
    groupId: o.groupId,
    front,
    back,
    ...(typeof questionResultId === 'number' ? { questionResultId } : {}),
  };
}

export function parseGroupCardList(value: unknown): GroupCard[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.map(parseGroupCard).filter((x): x is GroupCard => x !== null);
}

/** `POST /api/cards/cards` — camelCase JSON. */
export interface CreateCardPayload {
  readonly userId: string;
  readonly groupId: number;
  readonly frontLabel: string;
  readonly backLabel: string;
  readonly frontExample?: string | null;
  readonly frontComment?: string | null;
  readonly backExample?: string | null;
  readonly backComment?: string | null;
}

/** Nested sides for `PATCH /api/cards/{id}`. */
export interface UpdateCardSidePayload {
  readonly label: string;
  readonly example: string;
  readonly comment: string;
}

export interface UpdateCardPayload {
  readonly userId: string;
  readonly front: UpdateCardSidePayload;
  readonly back: UpdateCardSidePayload;
}
