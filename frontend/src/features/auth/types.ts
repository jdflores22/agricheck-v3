export interface AuthTokens {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export interface UserSummary {
  uuid: string
  email: string
  firstName: string
  lastName: string
  status: string
  emailVerified: boolean
  mustChangePassword: boolean
  roles: string[]
}

export interface AuthResponse {
  tokens: AuthTokens
  user: UserSummary
  redirectPath: string
}

export interface ApiEnvelope<T> {
  success: boolean
  data: T
  errors: Array<{ code: string; message: string }> | null
}

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  email: string
  password: string
  firstName: string
  lastName: string
  roleCode: string
}
