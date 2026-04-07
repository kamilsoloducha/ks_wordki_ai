# Wordki BFF - Project Structure

This backend follows a modular monolith architecture in .NET with three business modules:

- Users
- Cards
- Lessons

Each module is split into four layers:

- Api
- Application
- Domain
- Infrastructure

## Solution layout

```text
Bff.sln
src/
  Wordki.Bff.Api
  Wordki.Bff.SharedKernel
  Modules/
    Users/
      Wordki.Modules.Users.Api
      Wordki.Modules.Users.Application
      Wordki.Modules.Users.Domain
      Wordki.Modules.Users.Infrastructure
    Cards/
      Wordki.Modules.Cards.Api
      Wordki.Modules.Cards.Application
      Wordki.Modules.Cards.Domain
      Wordki.Modules.Cards.Infrastructure
    Lessons/
      Wordki.Modules.Lessons.Api
      Wordki.Modules.Lessons.Application
      Wordki.Modules.Lessons.Domain
      Wordki.Modules.Lessons.Infrastructure
tests/
  Wordki.Bff.Api.Tests
```

## Module responsibilities

- `Users` - registration, login, user roles (`User`, `Admin`), admin impersonation.
- `Cards` - groups, cards, and learning progress per card side (`Drawer`, `NextRepeat`, `Counter`).
- `Lessons` - lesson flow and storing answer attempts.

## Cross-module communication

- In-process events are used for module-to-module communication.
- Outbox is planned for durable event processing and crash recovery.
- Event handlers should be idempotent.

## Security and API host

- JWT authentication is implemented in API host (`Wordki.Bff.Api`).
- Role-based authorization policies are enforced in application/api layers.
- Impersonation actions should be audited.
