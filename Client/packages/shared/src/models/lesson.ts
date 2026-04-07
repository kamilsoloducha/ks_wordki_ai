/** `POST /api/lessons?userId=` */
export type CreateLessonPayload = {
  readonly lessonKind: string;
  readonly wordCount: number;
};

/** Odpowiedź z nagłówka Created — treść JSON z `Id` lekcji. */
export type CreateLessonResult = {
  readonly id: number;
};

/** `POST /api/lessons/{lessonId}/repetitions?userId=` */
export type AddLessonRepetitionPayload = {
  readonly questionResultId: number;
  /** `true` — „Znałem”, `false` — „Nie znałem”. */
  readonly result: boolean;
};

export type AddLessonRepetitionResult = {
  readonly id: number;
  readonly answeredAtUtc: string;
};
