# Wordki — klient (monorepo)

```
Client/
├── apps/
│   ├── web/          # React + TypeScript + Vite
│   └── mobile/       # Expo (Android / iOS)
├── packages/
│   └── shared/       # Wspólne typy, parsery, wywołania API → @wordki/shared
└── package.json      # npm workspaces
```

## Wspólny kod (`@wordki/shared`)

- **Modele** — m.in. `UserCardGroup`, typy auth (`RegisterUserPayload`, `LoginUserResult`, …).
- **Serwis BFF (axios)** — klasa `WordkiBackendService`: `register`, `login`, `getUserCardGroups`, `setAccessToken`.

Import w aplikacji:

```ts
import {
  WordkiBackendService,
  WordkiApiError,
  type UserCardGroup,
} from '@wordki/shared';

const api = new WordkiBackendService('http://localhost:5000');
await api.login({ email, password });
api.setAccessToken(token);
```

## Pierwsza instalacja

Z katalogu `Client/`:

```bash
npm install
```

## Web

```bash
npm run dev:web
```

**Uwaga:** Vite wczytuje tylko `.env` / `.env.local`, **nie** `.env.example`. Skopiuj `apps/web/.env.example` → `apps/web/.env` jeśli potrzebujesz zmiennych. Domyślnie (bez `VITE_BFF_BASE_URL`) żądania `/api` idą przez proxy Vite na `http://localhost:5000`.

## Mobile (Expo)

```bash
npm run dev:mobile
# lub
npm run android
```

Skopiuj `apps/mobile/.env.example` → `apps/mobile/.env`. Na emulatorze Android host to zwykle `10.0.2.2` zamiast `localhost`.

## Typecheck wspólnej paczki

```bash
npm run typecheck:shared
```
