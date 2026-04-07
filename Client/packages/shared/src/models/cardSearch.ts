/**
 * Filters for `GET /api/cards/search`.
 * The API also requires `getCount` and/or `getList` (default false each); use `WordkiBackendService.searchCards` or `searchCardsCount`.
 */
export type SearchCardsPayload = {
  userId: string;
  /** When set, only cards with at least one side in this drawer (results). */
  drawer?: number;
  /** When set, limits search to this group. */
  groupId?: number;
  /** 1-based page index (backend default: 1). */
  page?: number;
  /** Page size (backend default: 50, max: 500). */
  pageSize?: number;
};
