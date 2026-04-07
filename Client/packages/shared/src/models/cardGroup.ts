/** Request/response shapes for `POST/PATCH /api/cards/groups` (camelCase JSON). */
export interface CreateCardGroupPayload {
  readonly userId: string;
  readonly name: string;
  readonly frontSideType: string;
  readonly backSideType: string;
}

export type UpdateCardGroupPayload = CreateCardGroupPayload;

export interface CardGroupDto {
  readonly id: number;
  readonly name: string;
  readonly frontSideType: string;
  readonly backSideType: string;
}
