/** `GET /api/cards/side-type-pairs` — unordered pair; `sideType1` <= `sideType2` (ordinal). */
export type SideTypePairDto = {
  sideType1: string;
  sideType2: string;
};
