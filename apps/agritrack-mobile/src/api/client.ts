import { resolveApiBaseUrl } from '../config'
import { getApiBaseUrl, loadSession, saveSession } from '../storage/session'
import type { ApiEnvelope, AuthResponse, AuthTokens } from '../types/api'

export class ApiError extends Error {
  code: string

  constructor(code: string, message: string) {
    super(message)
    this.code = code
  }
}

export async function resolveBaseUrl() {
  return resolveApiBaseUrl(await getApiBaseUrl())
}

async function refreshTokens(refreshToken: string, baseUrl: string): Promise<AuthTokens | null> {
  const response = await fetch(`${baseUrl}/api/v1/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })

  const envelope = (await response.json()) as ApiEnvelope<AuthResponse>
  if (!response.ok || !envelope.success || !envelope.data?.tokens) {
    return null
  }

  await saveSession(envelope.data.tokens, envelope.data.user)
  return envelope.data.tokens
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
  retry = true,
): Promise<T> {
  const baseUrl = await resolveBaseUrl()
  const session = await loadSession()
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (!headers.has('Content-Type') && options.body && typeof options.body === 'string') {
    headers.set('Content-Type', 'application/json')
  }
  if (session.accessToken) {
    headers.set('Authorization', `Bearer ${session.accessToken}`)
  }

  const response = await fetch(`${baseUrl}${path}`, { ...options, headers })
  const envelope = (await response.json()) as ApiEnvelope<T>

  if (response.status === 401 && retry && session.refreshToken) {
    const tokens = await refreshTokens(session.refreshToken, baseUrl)
    if (tokens) {
      return apiRequest<T>(path, options, false)
    }
  }

  if (!response.ok || !envelope.success) {
    const message = envelope.errors?.[0]?.message ?? `Request failed (${response.status})`
    const code = envelope.errors?.[0]?.code ?? 'REQUEST_FAILED'
    throw new ApiError(code, message)
  }

  return envelope.data as T
}

export async function checkHealth(): Promise<boolean> {
  try {
    const baseUrl = await resolveBaseUrl()
    const response = await fetch(`${baseUrl}/api/mobile/health`)
    const envelope = (await response.json()) as ApiEnvelope<{ status: string }>
    return response.ok && envelope.success === true
  } catch {
    return false
  }
}
