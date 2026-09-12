import Constants from 'expo-constants'

export const PRODUCTION_API_BASE_URL = 'https://agricheck-v3-production.up.railway.app'

const extra = Constants.expoConfig?.extra as { apiBaseUrl?: string } | undefined

export function isLocalDevApiUrl(url: string) {
  const normalized = url.trim().toLowerCase()
  return (
    normalized.startsWith('http://') ||
    normalized.includes('localhost') ||
    normalized.includes('127.0.0.1') ||
    normalized.includes('10.0.2.2')
  )
}

export function defaultApiBaseUrl() {
  const fromExtra = extra?.apiBaseUrl?.replace(/\/$/, '')
  if (fromExtra) return fromExtra
  return PRODUCTION_API_BASE_URL
}

export function resolveApiBaseUrl(stored?: string | null) {
  const fallback = defaultApiBaseUrl()
  if (!stored) return fallback
  if (isLocalDevApiUrl(stored) && !isLocalDevApiUrl(fallback)) return fallback
  return stored.replace(/\/$/, '')
}

export const STORAGE_KEYS = {
  apiBaseUrl: 'agritrack.apiBaseUrl',
  accessToken: 'agritrack.accessToken',
  refreshToken: 'agritrack.refreshToken',
  userJson: 'agritrack.user',
} as const
