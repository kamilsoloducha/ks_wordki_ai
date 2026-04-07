import type { CardSide, GroupCard } from '@wordki/shared'
import type { LessonQuestionDirection } from './lessonTypes'

/** Pozycja w kolejce lekcji — strony pytanie / odpowiedź wg wybranego kierunku i typów grupy. */
export type LessonQueueItem = {
  readonly cardId: number
  readonly groupId: number
  /** Id wyniku SRS dla strony pytania — do `POST .../repetitions`. */
  readonly questionResultId: number
  readonly question: CardSide
  readonly answer: CardSide
}

/**
 * Dopasowuje strony karty do kierunku lekcji na podstawie typów stron grupy (front/back w DB).
 */
export function resolveQuestionAnswerSides(
  card: GroupCard,
  groupFrontType: string,
  groupBackType: string,
  direction: LessonQuestionDirection,
): { question: CardSide; answer: CardSide } {
  const matchesForward =
    groupFrontType === direction.questionSideType &&
    groupBackType === direction.answerSideType
  const matchesBackward =
    groupBackType === direction.questionSideType &&
    groupFrontType === direction.answerSideType
  if (matchesForward) {
    return { question: card.front, answer: card.back }
  }
  if (matchesBackward) {
    return { question: card.back, answer: card.front }
  }
  return { question: card.front, answer: card.back }
}

export function buildLessonQueue(
  cards: GroupCard[],
  groupTypes: Map<
    number,
    { frontSideType: string; backSideType: string }
  >,
  direction: LessonQuestionDirection,
): LessonQueueItem[] {
  const out: LessonQueueItem[] = []
  for (const card of cards) {
    if (typeof card.questionResultId !== 'number') {
      continue
    }
    const g = groupTypes.get(card.groupId)
    if (!g) continue
    const { question, answer } = resolveQuestionAnswerSides(
      card,
      g.frontSideType,
      g.backSideType,
      direction,
    )
    out.push({
      cardId: card.id,
      groupId: card.groupId,
      questionResultId: card.questionResultId,
      question,
      answer,
    })
  }
  return out
}
