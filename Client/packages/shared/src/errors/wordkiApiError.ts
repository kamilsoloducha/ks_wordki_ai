import { isAxiosError } from 'axios';

export interface ApiErrorItem {
  readonly code: string;
  readonly message: string;
  readonly field: string | null;
}

export class WordkiApiError extends Error {
  readonly status: number;
  readonly errors: readonly ApiErrorItem[];

  constructor(message: string, status: number, errors: readonly ApiErrorItem[]) {
    super(message);
    this.name = 'WordkiApiError';
    this.status = status;
    this.errors = errors;
  }
}

export function parseApiErrorBody(data: unknown): ApiErrorItem[] {
  if (data === null || typeof data !== 'object') {
    return [];
  }

  const raw = (data as { errors?: unknown }).errors;
  if (!Array.isArray(raw)) {
    return [];
  }

  const out: ApiErrorItem[] = [];
  for (const item of raw) {
    if (item === null || typeof item !== 'object') {
      continue;
    }
    const o = item as Record<string, unknown>;
    if (typeof o.code !== 'string' || typeof o.message !== 'string') {
      continue;
    }
    const fieldVal = o.field;
    const field =
      fieldVal === null || fieldVal === undefined
        ? null
        : typeof fieldVal === 'string'
          ? fieldVal
          : String(fieldVal);
    out.push({ code: o.code, message: o.message, field });
  }

  return out;
}

export function toWordkiApiError(error: unknown): WordkiApiError {
  if (isAxiosError(error)) {
    const status = error.response?.status ?? 0;
    const errors = parseApiErrorBody(error.response?.data);
    const message =
      errors[0]?.message ?? error.message ?? 'Request failed';
    return new WordkiApiError(message, status, errors);
  }

  if (error instanceof Error) {
    return new WordkiApiError(error.message, 0, []);
  }

  return new WordkiApiError('Unknown error', 0, []);
}
