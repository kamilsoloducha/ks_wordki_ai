import axios, { type AxiosInstance } from 'axios';
import { toWordkiApiError } from '../errors/wordkiApiError';
import type {
  LoginUserPayload,
  LoginUserResult,
  RegisterUserPayload,
  RegisterUserResult,
} from '../models/auth';
import type {
  CardGroupDto,
  CreateCardGroupPayload,
  UpdateCardGroupPayload,
} from '../models/cardGroup';
import {
  parseGroupCard,
  parseGroupCardList,
  type CreateCardPayload,
  type GroupCard,
  type UpdateCardPayload,
} from '../models/groupCard';
import { parseUserCardGroupList, type UserCardGroup } from '../models/userCardGroup';
import type { UserWordCountDto } from '../models/userWordCount';
import type { WordsDueTodayCountDto } from '../models/wordsDueTodayCount';
import type { SearchCardsPayload } from '../models/cardSearch';
import type { SideTypePairDto } from '../models/sideTypePair';
import type {
  AddLessonRepetitionPayload,
  AddLessonRepetitionResult,
  CreateLessonPayload,
  CreateLessonResult,
} from '../models/lesson';

/**
 * Klient HTTP do BFF (web + React Native). Używa axios: rejestracja, logowanie, karty.
 */
export class WordkiBackendService {
  private readonly http: AxiosInstance;

  constructor(baseURL: string) {
    const root = baseURL.replace(/\/$/, '');
    this.http = axios.create({
      baseURL: root,
      timeout: 30_000,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      validateStatus: (status) => status >= 200 && status < 300,
    });
  }

  /**
   * Ustawia nagłówek `Authorization: Bearer …` dla kolejnych żądań (np. po `login`).
   */
  setAccessToken(token: string | null): void {
    if (token) {
      this.http.defaults.headers.common.Authorization = `Bearer ${token}`;
    } else {
      delete this.http.defaults.headers.common.Authorization;
    }
  }

  async register(payload: RegisterUserPayload): Promise<RegisterUserResult> {
    try {
      const { data } = await this.http.post<RegisterUserResult>(
        '/api/users/register',
        payload,
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  async login(payload: LoginUserPayload): Promise<LoginUserResult> {
    try {
      const { data } = await this.http.post<LoginUserResult>(
        '/api/users/login',
        payload,
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `GET /api/cards/groups/{groupId}/cards?userId=` */
  async getGroupCards(userId: string, groupId: number): Promise<GroupCard[]> {
    try {
      const { data } = await this.http.get<unknown>(
        `/api/cards/groups/${groupId}/cards`,
        { params: { userId } },
      );
      return parseGroupCardList(data);
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `GET /api/cards/groups?userId=` */
  async getUserCardGroups(userId: string): Promise<UserCardGroup[]> {
    try {
      const { data } = await this.http.get<unknown>('/api/cards/groups', {
        params: { userId },
      });
      return parseUserCardGroupList(data);
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /**
   * `GET /api/cards/side-type-pairs?userId=` — unikalne pary typów stron z grup użytkownika
   * (kierunek bez znaczenia: EN+PL i PL+EN → jedna para).
   */
  async getDistinctSideTypePairs(userId: string): Promise<SideTypePairDto[]> {
    try {
      const { data } = await this.http.get<SideTypePairDto[]>(
        '/api/cards/side-type-pairs',
        { params: { userId } },
      );
      return Array.isArray(data) ? data : [];
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `GET /api/cards/words-count?userId=` — łączna liczba kart (słów) we wszystkich grupach użytkownika. */
  async getUserWordCount(userId: string): Promise<UserWordCountDto> {
    try {
      const { data } = await this.http.get<UserWordCountDto>(
        '/api/cards/words-count',
        { params: { userId } },
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /**
   * `GET /api/cards/due-today-count?userId=` — cards with at least one side whose `next_repeat_utc` is before
   * tomorrow 00:00 UTC (includes overdue from earlier days). Optional direction filter on group front/back types.
   */
  /**
   * `GET /api/cards/due-today` — karty z powtórką do końca dziś (UTC), opcjonalnie filtrowane
   * po kierunku jak licznik; kolejność jak w wyszukiwaniu (groupId, id), max `limit` kart.
   */
  async getDueTodayCards(
    userId: string,
    options: {
      direction?: { questionSideType: string; answerSideType: string } | null;
      limit: number;
      /** Domyślnie `review` — powtórki; `newWords` — strona pytania bez `next_repeat_utc`. */
      wordSource?: 'review' | 'newWords';
    },
  ): Promise<GroupCard[]> {
    try {
      const { data } = await this.http.get<unknown>('/api/cards/due-today', {
        params: {
          userId,
          limit: options.limit,
          ...(options.wordSource &&
            options.wordSource !== 'review' && { wordSource: options.wordSource }),
          ...(options.direction && {
            questionSideType: options.direction.questionSideType,
            answerSideType: options.direction.answerSideType,
          }),
        },
      });
      return parseGroupCardList(data);
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  async getWordsDueTodayCount(
    userId: string,
    direction?: { questionSideType: string; answerSideType: string } | null,
    wordSource: 'review' | 'newWords' = 'review',
  ): Promise<WordsDueTodayCountDto> {
    try {
      const { data } = await this.http.get<WordsDueTodayCountDto>(
        '/api/cards/due-today-count',
        {
          params: {
            userId,
            ...(wordSource !== 'review' && { wordSource }),
            ...(direction && {
              questionSideType: direction.questionSideType,
              answerSideType: direction.answerSideType,
            }),
          },
        },
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /**
   * `GET /api/cards/search` with `getList=true`.
   * Optional `drawer` / `groupId` — omit to disable that filter.
   */
  async searchCards(payload: SearchCardsPayload): Promise<GroupCard[]> {
    try {
      const { data } = await this.http.get<unknown>('/api/cards/search', {
        params: {
          userId: payload.userId,
          ...(payload.drawer !== undefined && { drawer: payload.drawer }),
          ...(payload.groupId !== undefined && { groupId: payload.groupId }),
          ...(payload.page !== undefined && { page: payload.page }),
          ...(payload.pageSize !== undefined && { pageSize: payload.pageSize }),
          getList: true,
        },
      });
      return parseGroupCardList(data);
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /**
   * `GET /api/cards/search` with `getCount=true` — returns the number of cards matching the same filters as `searchCards`.
   */
  async searchCardsCount(payload: SearchCardsPayload): Promise<number> {
    try {
      const { data } = await this.http.get<{ count: number }>('/api/cards/search', {
        params: {
          userId: payload.userId,
          ...(payload.drawer !== undefined && { drawer: payload.drawer }),
          ...(payload.groupId !== undefined && { groupId: payload.groupId }),
          ...(payload.page !== undefined && { page: payload.page }),
          ...(payload.pageSize !== undefined && { pageSize: payload.pageSize }),
          getCount: true,
        },
      });
      return data.count;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `POST /api/cards/groups` */
  async createCardGroup(
    payload: CreateCardGroupPayload,
  ): Promise<CardGroupDto> {
    try {
      const { data } = await this.http.post<CardGroupDto>(
        '/api/cards/groups',
        payload,
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `PATCH /api/cards/groups/{id}` */
  async updateCardGroup(
    groupId: number,
    payload: UpdateCardGroupPayload,
  ): Promise<CardGroupDto> {
    try {
      const { data } = await this.http.patch<CardGroupDto>(
        `/api/cards/groups/${groupId}`,
        payload,
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `POST /api/cards/cards` */
  async createCard(payload: CreateCardPayload): Promise<GroupCard> {
    try {
      const { data } = await this.http.post<unknown>(
        '/api/cards/cards',
        payload,
      );
      const parsed = parseGroupCard(data);
      if (!parsed) {
        throw new Error('Invalid create card response');
      }
      return parsed;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `PATCH /api/cards/{id}` */
  async updateCard(cardId: number, payload: UpdateCardPayload): Promise<GroupCard> {
    try {
      const { data } = await this.http.patch<unknown>(
        `/api/cards/${cardId}`,
        payload,
      );
      const parsed = parseGroupCard(data);
      if (!parsed) {
        throw new Error('Invalid update card response');
      }
      return parsed;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `DELETE /api/cards/{cardId}?userId=` */
  async deleteCard(userId: string, cardId: number): Promise<void> {
    try {
      await this.http.delete(`/api/cards/${cardId}`, {
        params: { userId },
      });
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `PUT /api/cards/tick` — ustawia `is_ticked = true` dla wiersza `cards.results`. */
  async tickCardResult(userId: string, resultId: number): Promise<void> {
    try {
      await this.http.put('/api/cards/tick', {
        userId,
        resultId,
      });
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `POST /api/lessons?userId=` — tworzy rekord lekcji (np. przed sesją fiszek). */
  async createLesson(
    userId: string,
    payload: CreateLessonPayload,
  ): Promise<CreateLessonResult> {
    try {
      const { data } = await this.http.post<CreateLessonResult>('/api/lessons', payload, {
        params: { userId },
      });
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }

  /** `POST /api/lessons/{lessonId}/repetitions?userId=` — zapis odpowiedzi w ramach lekcji. */
  async addLessonRepetition(
    userId: string,
    lessonId: number,
    payload: AddLessonRepetitionPayload,
  ): Promise<AddLessonRepetitionResult> {
    try {
      const { data } = await this.http.post<AddLessonRepetitionResult>(
        `/api/lessons/${lessonId}/repetitions`,
        {
          questionResultId: payload.questionResultId,
          result: payload.result,
        },
        { params: { userId } },
      );
      return data;
    } catch (e) {
      throw toWordkiApiError(e);
    }
  }
}
