import type { AuthState } from './authSlice'

const EXPIRY_SKEW_MS = 30_000

function readJwtExpiry(accessToken: string | null | undefined): string | null {
  if (!accessToken) return null
  const parts = accessToken.split('.')
  if (parts.length < 2) return null
  try {
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))) as { exp?: number }
    if (typeof payload.exp === 'number') {
      return new Date(payload.exp * 1000).toISOString()
    }
  } catch {
    return null
  }
  return null
}

export function resolveAccessTokenExpiry(
  accessToken: string | null | undefined,
  storedExpiresAt: string | null | undefined,
): string | null {
  return storedExpiresAt ?? readJwtExpiry(accessToken)
}

export function isTokenExpired(expiresAt: string | null | undefined, skewMs = EXPIRY_SKEW_MS): boolean {
  if (!expiresAt) return false
  const expires = Date.parse(expiresAt)
  if (Number.isNaN(expires)) return false
  return Date.now() >= expires - skewMs
}

export function hasStoredAuthTokens(auth: Pick<AuthState, 'accessToken' | 'refreshToken'>): boolean {
  return Boolean(auth.accessToken && auth.refreshToken)
}

export function hasUsableSession(
  auth: Pick<AuthState, 'accessToken' | 'refreshToken' | 'refreshTokenExpiresAt'>,
): boolean {
  if (!hasStoredAuthTokens(auth)) return false
  if (isTokenExpired(auth.refreshTokenExpiresAt)) return false
  return true
}

export function shouldRefreshAccessToken(
  auth: Pick<AuthState, 'accessToken' | 'refreshToken' | 'accessTokenExpiresAt' | 'refreshTokenExpiresAt'>,
): boolean {
  if (!hasUsableSession(auth)) return false
  const accessExpiry = resolveAccessTokenExpiry(auth.accessToken, auth.accessTokenExpiresAt)
  return isTokenExpired(accessExpiry)
}
