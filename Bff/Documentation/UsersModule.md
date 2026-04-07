# Users Module

## Purpose

Users module is responsible for:

- user registration
- user email confirmation
- user login
- user account removal
- user roles (`User`, `Admin`)
- admin impersonation of another user

This module is a part of the modular monolith and follows layer split:

- `Wordki.Modules.Users.Api`
- `Wordki.Modules.Users.Application`
- `Wordki.Modules.Users.Domain`
- `Wordki.Modules.Users.Infrastructure`

## Domain assumptions

Current user model contains basic user information:

- `Id`
- `Email`
- `UserName`
- `Role`

Role is represented by enum:

- `User`
- `Admin`

## Authorization and impersonation

Authorization assumptions:

- normal users can work only on their own resources
- admin can execute privileged operations
- impersonation must be audited

Recommended impersonation flow:

1. admin authenticates with own account
2. admin requests impersonation token for selected user
3. system issues token with:
   - effective user id (target user)
   - actor/admin id (original admin)
   - impersonation flag
4. every write action stores audit data (`actorId`, `effectiveUserId`)

## API layer (current status)

The module currently exposes a basic health endpoint:

- `GET /api/users/health`

Planned endpoints:

- `POST /api/users/register`
- `POST /api/users/confirm`
- `POST /api/users/login`
- `DELETE /api/users/me`
- `POST /api/users/impersonate`
- `GET /api/users/me`

## Available actions

### Register

Goal:

- create new user account in `PendingConfirmation` state

Expected flow:

1. validate email and password
2. create user record
3. create confirmation token
4. send confirmation email

Result:

- user exists, but cannot log in until email is confirmed

### Confirm

Goal:

- confirm user identity by email token

Expected flow:

1. user clicks confirmation link from email
2. backend validates token and expiration
3. account status changes to `Active`

Result:

- user can log in

### Login

Goal:

- authenticate user and issue JWT

Expected flow:

1. validate credentials
2. check if account is confirmed and active
3. issue access token (and refresh token if enabled)

Result:

- authenticated session for API calls

### Remove

Goal:

- remove current user account

Expected flow:

1. authenticate request
2. optionally require password re-check
3. soft-delete or hard-delete account (project decision)
4. revoke active sessions/tokens

Result:

- account removed and user can no longer log in

## Integration with other modules

Users module provides identity context used by:

- `Cards` (resource ownership, user groups)
- `Lessons` (lesson ownership, answer ownership)

Cross-module communication is based on:

- in-process events
- outbox for durable processing

## Next implementation steps

1. add JWT auth configuration in `Wordki.Bff.Api`
2. add register/login use cases in `Users.Application`
3. add password hashing and user repository in `Users.Infrastructure`
4. add impersonation use case with audit log
5. add role-based authorization policies
