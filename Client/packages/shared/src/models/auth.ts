/** Body JSON — camelCase jak w ASP.NET (`RegisterUserRequest`). */
export interface RegisterUserPayload {
  readonly email: string;
  readonly password: string;
  readonly userName: string;
}

export interface RegisterUserResult {
  readonly userId: string;
  readonly email: string;
  readonly status: string;
}

export interface LoginUserPayload {
  readonly email: string;
  readonly password: string;
}

export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly role: string;
  readonly status: string;
}

export interface LoginUserResult {
  readonly accessToken: string;
  readonly tokenType: string;
  /** ISO 8601 z API (`DateTime` z BFF). */
  readonly expiresAtUtc: string;
  readonly user: CurrentUser;
}
